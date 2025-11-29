// frontend/app/(tabs)/new-trip.tsx
import React, { useState, useEffect, useRef } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  ScrollView,
  Modal,
  SafeAreaView,
  Alert,
  ActivityIndicator,
  TextInput,
} from 'react-native';
import { useRouter } from 'expo-router';
import { api } from '@/lib/api';
import { IconSymbol } from '@/components/ui/icon-symbol';



export default function NewTripScreen() {
  const router = useRouter();

  const [destination, setDestination] = useState('');
  const [country, setCountry] = useState('');
  const [days, setDays] = useState('');
  const [selectedThemes, setSelectedThemes] = useState<string[]>([]);
  const [selectedBudgets, setSelectedBudgets] = useState<string[]>([]);
  const [selectedIntensities, setSelectedIntensities] = useState<string[]>([]);
  const [selectedTransports, setSelectedTransports] = useState<string[]>([]);
  const [showDaysDropdown, setShowDaysDropdown] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [loadingStep, setLoadingStep] = useState(0);
  const [isLoadingLocation, setIsLoadingLocation] = useState(false);
  const destinationTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Loading messages
  const loadingMessages = [
    'Checking location details...',
    'Processing data...',
    'Finding the best path...',
    'Preparing the map...',
  ];

  // Loading animation effect
  useEffect(() => {
    let interval: ReturnType<typeof setInterval> | undefined;
    if (isLoading) {
      setLoadingStep(0);
      interval = setInterval(() => {
        setLoadingStep((prev) => (prev + 1) % loadingMessages.length);
      }, 2000); // Her 2 saniyede bir mesaj değişir
    }
    return () => {
      if (interval) clearInterval(interval);
    };
  }, [isLoading, loadingMessages.length]);

  // Fetch location info from Google Places API when destination changes
  useEffect(() => {
    // Clear previous timeout
    if (destinationTimeoutRef.current) {
      clearTimeout(destinationTimeoutRef.current);
    }

    // If destination is empty, clear country and city
    if (!destination.trim()) {
      setCountry('');
      return;
    }

    // Debounce API call - wait 1 second after user stops typing
    destinationTimeoutRef.current = setTimeout(async () => {
      if (destination.trim().length < 3) {
        return; // Don't search for very short queries
      }

      try {
        setIsLoadingLocation(true);
        const response = await api.get('/api/Places/location-info', {
          params: { destination: destination.trim() },
        });

        if (response.data) {
          if (response.data.country) {
            setCountry(response.data.country);
          }
          // Note: We don't set city here as it's extracted from destination
          // The destination itself serves as the city/region
        }
      } catch (error: any) {
        console.error('Error fetching location info:', error);
        // Don't show error to user, just log it
        // User can still proceed with manual selection if needed
      } finally {
        setIsLoadingLocation(false);
      }
    }, 1000);

    return () => {
      if (destinationTimeoutRef.current) {
        clearTimeout(destinationTimeoutRef.current);
      }
    };
  }, [destination]);

  // Themes based on MULTI_SELECTION_GUIDE.md enum values
  const themes = [
    { label: 'Nature', value: 0 },
    { label: 'Sea', value: 1 },
    { label: 'History', value: 2 },
    { label: 'Beach', value: 3 },
    { label: 'Food', value: 4 },
    { label: 'Photospot', value: 5 },
  ];

  // Budgets based on MULTI_SELECTION_GUIDE.md enum values
  const budgets = [
    { label: 'Low', value: 0 },
    { label: 'Medium', value: 1 },
    { label: 'High', value: 2 },
  ];

  // Intensities based on MULTI_SELECTION_GUIDE.md enum values
  const intensities = [
    { label: 'Relaxed', value: 0 },
    { label: 'Active', value: 1 },
  ];

  // Transports based on MULTI_SELECTION_GUIDE.md enum values
  const transports = [
    { label: 'Car', value: 0 },
    { label: 'Walk', value: 1 },
    { label: 'Public Transport', value: 2 },
  ];


  const toggleTheme = (theme: string) => {
    setSelectedThemes(prev =>
      prev.includes(theme) ? prev.filter(t => t !== theme) : [...prev, theme],
    );
  };

  const toggleBudget = (budget: string) => {
    setSelectedBudgets(prev =>
      prev.includes(budget) ? prev.filter(b => b !== budget) : [...prev, budget],
    );
  };

  const toggleIntensity = (intensity: string) => {
    setSelectedIntensities(prev =>
      prev.includes(intensity) ? prev.filter(i => i !== intensity) : [...prev, intensity],
    );
  };

  const toggleTransport = (transport: string) => {
    setSelectedTransports(prev =>
      prev.includes(transport) ? prev.filter(t => t !== transport) : [...prev, transport],
    );
  };

  const handlePlanTrip = async () => {
    console.log('=== handlePlanTrip STARTED ===');
    
    // Validation
    if (!destination.trim()) {
      Alert.alert('Error', 'Please enter a destination');
      return;
    }
    if (!country.trim()) {
      Alert.alert('Error', 'Please wait for country to be detected, or try a different destination');
      return;
    }
    if (!days) {
      Alert.alert('Error', 'Please select number of days');
      return;
    }
    if (selectedThemes.length === 0) {
      Alert.alert('Error', 'Please select at least one theme');
      return;
    }
    if (selectedBudgets.length === 0) {
      Alert.alert('Error', 'Please select at least one budget level');
      return;
    }
    if (selectedIntensities.length === 0) {
      Alert.alert('Error', 'Please select at least one intensity level');
      return;
    }
    if (selectedTransports.length === 0) {
      Alert.alert('Error', 'Please select at least one transport mode');
      return;
    }

    console.log('Validation passed, starting request...');
    setIsLoading(true);

    try {
      // Convert selected theme labels to enum values
      const themeValues = selectedThemes.map(themeLabel => 
        themes.find(t => t.label === themeLabel)?.value ?? 0
      );

      // Convert selected budget labels to enum values
      const budgetValues = selectedBudgets.map(budgetLabel => 
        budgets.find(b => b.label === budgetLabel)?.value ?? 1
      );

      // Convert selected intensity labels to enum values
      const intensityValues = selectedIntensities.map(intensityLabel => 
        intensities.find(i => i.label === intensityLabel)?.value ?? 0
      );

      // Convert selected transport labels to enum values
      const transportValues = selectedTransports.map(transportLabel => 
        transports.find(t => t.label === transportLabel)?.value ?? 0
      );

      // Backend request with proper parameter order (as per MULTI_SELECTION_GUIDE.md)
      const requestBody = {
        region: destination,
        days: Number.parseInt(days),
        themes: themeValues,
        budgets: budgetValues,
        intensities: intensityValues,
        transports: transportValues,
      };

      console.log('Sending request to backend:', requestBody);

      const response = await api.post('/api/Routes/plan', requestBody);

      console.log('Backend response:', response.data);
      
      const itineraryId = response.data.itineraryId;
      
      if (!itineraryId) {
        throw new Error('No itinerary ID received from backend');
      }

      // Trip detail sayfasına yönlendir
      router.push({
        pathname: '/trip-detail',
        params: {
          itineraryId: itineraryId.toString(),
        },
      });

    } catch (error: any) {
      console.error('Error creating trip:', error);
      console.error('Error response:', error.response?.data);
      console.error('Error code:', error.code);
      console.error('Error message:', error.message);
      
      let errorMessage = 'Failed to create trip. Please try again.';
      
      if (error.code === 'ECONNABORTED') {
        errorMessage = 'Request timeout. The AI is taking longer than expected. Please try again.';
      } else if (error.code === 'ERR_NETWORK') {
        errorMessage = 'Network error. Please check your connection and make sure the backend is running.';
      } else if (error.response?.status === 401) {
        errorMessage = 'Please login first to create a trip.';
      } else if (error.response?.data?.message) {
        errorMessage = error.response.data.message;
      } else if (error.message) {
        errorMessage = error.message;
      }
      
      Alert.alert('Error', errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <SafeAreaView style={styles.root}>
      <ScrollView contentContainerStyle={styles.scroll}>
        {/* Header */}
        <View style={styles.header}>
          <View style={styles.headerContent}>
            <Text style={styles.headerTitle}>Plan Your Trip</Text>
            <Text style={styles.headerSubtitle}>Create your perfect trip itinerary</Text>
          </View>
          <TouchableOpacity 
            style={styles.refreshButton}
            onPress={() => {
              // Reset form
              setDestination('');
              setCountry('');
              setDays('');
              setSelectedThemes([]);
              setSelectedBudgets([]);
              setSelectedIntensities([]);
              setSelectedTransports([]);
            }}
          >
            <IconSymbol name="arrow.clockwise" size={20} color="#0d9488" />
          </TouchableOpacity>
        </View>

        {/* City / Region / Destination */}
        <View style={styles.field}>
          <Text style={styles.label}>Destination</Text>
          <View style={styles.inputContainer}>
            <TextInput
              style={styles.input}
              placeholder="Enter destination (e.g., Istanbul, Paris, New York)"
              placeholderTextColor="#9ca3af"
              value={destination}
              onChangeText={setDestination}
            />
            {isLoadingLocation && (
              <ActivityIndicator size="small" color="#0d9488" style={styles.inputLoader} />
            )}
          </View>
          {destination && !isLoadingLocation && (
            <Text style={styles.hintText}>
              {country ? `Detected: ${country}` : 'Searching location...'}
            </Text>
          )}
        </View>

        {/* Country - Auto-filled from Google API */}
        <View style={styles.field}>
          <Text style={styles.label}>Country</Text>
          <View style={[styles.select, styles.selectReadOnly]}>
            <Text style={[styles.selectText, !country && styles.placeholderText]}>
              {country || 'Will be detected from destination'}
            </Text>
            {country && <Text style={styles.checkIcon}>✓</Text>}
          </View>
        </View>

        {/* Days */}
        <View style={styles.field}>
          <Text style={styles.label}>How many days?</Text>
          <TouchableOpacity
            style={styles.select}
            onPress={() => setShowDaysDropdown(true)}
          >
            <Text style={[styles.selectText, !days && styles.placeholderText]}>
              {days || 'Select days'}
            </Text>
            <Text style={styles.selectIcon}>˅</Text>
          </TouchableOpacity>
        </View>

        {/* Days Modal */}
        <Modal visible={showDaysDropdown} transparent animationType="fade">
          <View style={styles.modalOverlay}>
            <View style={styles.modalBoxLarge}>
              <View style={styles.modalHeader}>
                <Text style={styles.modalHeaderText}>Select Days</Text>
              </View>
              <ScrollView style={styles.modalScroll}>
                {Array.from({ length: 20 }, (_, i) => (i + 1).toString()).map(num => (
                  <TouchableOpacity
                    key={num}
                    style={styles.modalItem}
                    onPress={() => {
                      setDays(num);
                      setShowDaysDropdown(false);
                    }}
                  >
                    <Text style={styles.modalItemText}>{num} {num === '1' ? 'day' : 'days'}</Text>
                  </TouchableOpacity>
                ))}
              </ScrollView>
              <TouchableOpacity
                style={styles.modalCancel}
                onPress={() => setShowDaysDropdown(false)}
              >
                <Text style={styles.modalCancelText}>Cancel</Text>
              </TouchableOpacity>
            </View>
          </View>
        </Modal>

        {/* Themes */}
        <View style={styles.field}>
          <Text style={styles.label}>Select themes (multiple allowed)</Text>
          <View style={styles.chipRow}>
            {themes.map(theme => {
              const active = selectedThemes.includes(theme.label);
              return (
                <TouchableOpacity
                  key={theme.value}
                  onPress={() => toggleTheme(theme.label)}
                  style={[styles.chip, active && styles.chipActive]}
                >
                  <Text style={[styles.chipText, active && styles.chipTextActive]}>
                    {theme.label}
                  </Text>
                </TouchableOpacity>
              );
            })}
          </View>
        </View>

        {/* Budget */}
        <View style={styles.field}>
          <Text style={styles.label}>Budget (multiple allowed)</Text>
          <View style={styles.chipRow}>
            {budgets.map(budget => {
              const active = selectedBudgets.includes(budget.label);
              return (
                <TouchableOpacity
                  key={budget.value}
                  onPress={() => toggleBudget(budget.label)}
                  style={[styles.chipWide, active && styles.chipActive]}
                >
                  <Text style={[styles.chipText, active && styles.chipTextActive]}>
                    {budget.label}
                  </Text>
                </TouchableOpacity>
              );
            })}
          </View>
        </View>


        {/* Activity Level (Intensity) */}
        <View style={styles.field}>
          <Text style={styles.label}>Activity level (multiple allowed)</Text>
          <View style={styles.chipRow}>
            {intensities.map(intensity => {
              const active = selectedIntensities.includes(intensity.label);
              return (
                <TouchableOpacity
                  key={intensity.value}
                  onPress={() => toggleIntensity(intensity.label)}
                  style={[styles.chipWide, active && styles.chipActive]}
                >
                  <Text style={[styles.chipText, active && styles.chipTextActive]}>
                    {intensity.label}
                  </Text>
                </TouchableOpacity>
              );
            })}
          </View>
        </View>

        {/* Transport */}
        <View style={styles.field}>
          <Text style={styles.label}>Transport (multiple allowed)</Text>
          <View style={styles.chipRow}>
            {transports.map(transport => {
              const active = selectedTransports.includes(transport.label);
              return (
                <TouchableOpacity
                  key={transport.value}
                  onPress={() => toggleTransport(transport.label)}
                  style={[styles.chipWide, active && styles.chipActive]}
                >
                  <Text style={[styles.chipText, active && styles.chipTextActive]}>
                    {transport.label}
                  </Text>
                </TouchableOpacity>
              );
            })}
          </View>
        </View>

        {/* Plan Trip Button */}
        <TouchableOpacity 
          style={[styles.planBtn, isLoading && styles.planBtnDisabled]} 
          onPress={handlePlanTrip}
          disabled={isLoading}
        >
          <Text style={styles.planBtnText}>Plan Trip</Text>
        </TouchableOpacity>
      </ScrollView>

      {/* Loading Modal */}
      <Modal
        visible={isLoading}
        transparent
        animationType="fade"
      >
        <View style={styles.loadingOverlay}>
          <View style={styles.loadingContainer}>
            <ActivityIndicator size="large" color="#0d9488" />
            <Text style={styles.loadingText}>{loadingMessages[loadingStep]}</Text>
            <View style={styles.loadingDots}>
              {loadingMessages.map((msg, index) => (
                <View
                  key={`dot-${msg}-${index}`}
                  style={[
                    styles.dot,
                    index === loadingStep && styles.dotActive,
                  ]}
                />
              ))}
            </View>
          </View>
        </View>
      </Modal>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  root: {
    flex: 1,
    backgroundColor: '#f9fafb',
  },
  scroll: {
    paddingHorizontal: 20,
    paddingBottom: 40,
    paddingTop: 16, // Back yazısını aşağı çekiyor
  },
  header: {
    marginBottom: 32,
    paddingTop: 8,
    flexDirection: 'row',
    alignItems: 'flex-start',
    justifyContent: 'space-between',
  },
  headerContent: {
    flex: 1,
  },
  headerTitle: {
    fontSize: 32,
    fontWeight: '800',
    color: '#0d9488',
    marginBottom: 4,
  },
  headerSubtitle: {
    fontSize: 16,
    color: '#6b7280',
    fontWeight: '400',
  },
  refreshButton: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: '#F5F5F7',
    alignItems: 'center',
    justifyContent: 'center',
    marginTop: 4,
  },
  field: {
    marginBottom: 24,
  },
  label: {
    fontSize: 15,
    fontWeight: '600',
    color: '#374151',
    marginBottom: 10,
  },
  inputContainer: {
    position: 'relative',
    flexDirection: 'row',
    alignItems: 'center',
  },
  input: {
    flex: 1,
    borderRadius: 12,
    paddingHorizontal: 16,
    paddingVertical: 14,
    backgroundColor: '#ffffff',
    color: '#111827',
    fontSize: 16,
    borderWidth: 1,
    borderColor: '#e5e7eb',
  },
  inputLoader: {
    position: 'absolute',
    right: 16,
  },
  hintText: {
    marginTop: 6,
    fontSize: 13,
    color: '#0d9488',
    fontStyle: 'italic',
  },
  selectReadOnly: {
    backgroundColor: '#f9fafb',
    borderColor: '#d1d5db',
  },
  checkIcon: {
    fontSize: 18,
    color: '#10b981',
    fontWeight: 'bold',
  },
  select: {
    borderRadius: 12,
    paddingHorizontal: 16,
    paddingVertical: 14,
    backgroundColor: '#ffffff',
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    borderWidth: 1,
    borderColor: '#e5e7eb',
  },
  selectDisabled: {
    backgroundColor: '#f3f4f6',
    opacity: 0.6,
  },
  selectText: {
    fontSize: 16,
    color: '#111827',
  },
  selectIcon: {
    fontSize: 16,
    color: '#4b5563',
  },
  chipRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
  },
  chip: {
    paddingHorizontal: 18,
    paddingVertical: 10,
    borderRadius: 20,
    backgroundColor: '#f0fdfa',
    borderWidth: 1.5,
    borderColor: '#99f6e4',
  },
  chipWide: {
    flex: 1,
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderRadius: 20,
    backgroundColor: '#f0fdfa',
    alignItems: 'center',
    borderWidth: 1.5,
    borderColor: '#99f6e4',
  },
  chipActive: {
    backgroundColor: '#0d9488',
    borderColor: '#0d9488',
  },
  chipText: {
    fontSize: 15,
    color: '#0f766e',
    fontWeight: '600',
  },
  chipTextActive: {
    color: '#ffffff',
  },
  planBtn: {
    marginTop: 24,
    borderRadius: 16,
    backgroundColor: '#0d9488',
    paddingVertical: 16,
    alignItems: 'center',
    flexDirection: 'row',
    justifyContent: 'center',
    gap: 10,
    shadowColor: '#0d9488',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.3,
    shadowRadius: 8,
    elevation: 5,
  },
  planBtnDisabled: {
    opacity: 0.6,
  },
  planBtnText: {
    color: '#ffffff',
    fontWeight: '700',
    fontSize: 17,
  },
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(15, 23, 42, 0.4)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  modalBox: {
    width: 200,
    backgroundColor: '#ffffff',
    borderRadius: 16,
    paddingVertical: 8,
  },
  modalBoxLarge: {
    width: '80%',
    maxHeight: '70%',
    backgroundColor: '#ffffff',
    borderRadius: 16,
    overflow: 'hidden',
  },
  modalHeader: {
    paddingVertical: 16,
    paddingHorizontal: 20,
    backgroundColor: '#f9fafb',
    borderBottomWidth: 1,
    borderBottomColor: '#e5e7eb',
  },
  modalHeaderText: {
    fontSize: 18,
    fontWeight: '700',
    color: '#0d9488',
    textAlign: 'center',
  },
  modalScroll: {
    maxHeight: 400,
  },
  modalItem: {
    paddingVertical: 14,
    paddingHorizontal: 20,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: '#f3f4f6',
  },
  modalItemText: {
    fontSize: 16,
    color: '#111827',
  },
  modalCancel: {
    paddingVertical: 14,
    borderTopWidth: 1,
    borderTopColor: '#e5e7eb',
    alignItems: 'center',
    backgroundColor: '#ffffff',
  },
  modalCancelText: {
    color: '#ef4444',
    fontWeight: '600',
    fontSize: 16,
  },
  budgetValue: {
    marginTop: 6,
    fontSize: 16,
    fontWeight: '600',
    color: '#0f766e',
    alignSelf: 'center',
  },
  placeholderText: {
    color: '#9ca3af',
  },
  loadingOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  loadingContainer: {
    backgroundColor: '#ffffff',
    borderRadius: 20,
    padding: 40,
    alignItems: 'center',
    minWidth: 280,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.3,
    shadowRadius: 8,
    elevation: 10,
  },
  loadingText: {
    marginTop: 20,
    fontSize: 16,
    color: '#4b5563',
    fontWeight: '500',
    textAlign: 'center',
  },
  loadingDots: {
    flexDirection: 'row',
    marginTop: 20,
    gap: 8,
  },
  dot: {
    width: 8,
    height: 8,
    borderRadius: 4,
    backgroundColor: '#d1d5db',
  },
  dotActive: {
    backgroundColor: '#0d9488',
  },
});
