import { StyleSheet, Text, View, ScrollView, TextInput, TouchableOpacity, Dimensions } from 'react-native';
import { StatusBar } from 'expo-status-bar';
import { IconSymbol } from '@/components/ui/icon-symbol';

const { width } = Dimensions.get('window');

export default function HomeScreen() {
  return (
    <View style={styles.container}>
      <StatusBar style="dark" />
      
      <ScrollView style={styles.scrollView} showsVerticalScrollIndicator={false}>
        {/* Header */}
        <View style={styles.header}>
          <Text style={styles.greeting}>Hello, Ayşe 👋</Text>
          <TouchableOpacity style={styles.notificationButton}>
            <IconSymbol name="bell" size={24} color="#333" />
          </TouchableOpacity>
        </View>

        {/* Search Bar */}
        <View style={styles.searchContainer}>
          <IconSymbol name="magnifyingglass" size={20} color="#8E8E8F" />
          <TextInput
            style={styles.searchInput}
            placeholder="Where do you want to go?"
            placeholderTextColor="#8E8E8F"
          />
        </View>

        {/* Popular Trips */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Popular Trips</Text>
          
          <ScrollView 
            horizontal 
            showsHorizontalScrollIndicator={false}
            style={styles.horizontalScroll}
          >
            <View style={styles.tripCard}>
              <View style={styles.tripImage}>
                <View style={styles.imagePlaceholder} />
              </View>
              <Text style={styles.tripTitle}>Kaputaş Beach</Text>
              <Text style={styles.tripDetails}>46 ln • 16 m</Text>
            </View>

            <View style={styles.tripCard}>
              <View style={styles.tripImage}>
                <View style={[styles.imagePlaceholder, { backgroundColor: '#D2B48C' }]} />
              </View>
              <Text style={styles.tripTitle}>Uchisar Castle</Text>
              <Text style={styles.tripDetails}>35 m</Text>
            </View>
          </ScrollView>
        </View>

        {/* Suggestion Card */}
        <View style={styles.suggestionCard}>
          <View style={styles.suggestionIcon}>
            <IconSymbol name="lightbulb" size={24} color="#5C9B9B" />
          </View>
          <View style={styles.suggestionText}>
            <Text style={styles.suggestionTitle}>Would you like a coastal</Text>
            <Text style={styles.suggestionSubtitle}>trip this week?</Text>
          </View>
        </View>

      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#FFFFFF',
  },
  scrollView: {
    flex: 1,
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: 20,
    paddingTop: 60,
    paddingBottom: 20,
  },
  greeting: {
    fontSize: 24,
    fontWeight: '600',
    color: '#333333',
  },
  notificationButton: {
    padding: 8,
  },
  searchContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#F8F9FA',
    marginHorizontal: 20,
    paddingHorizontal: 15,
    paddingVertical: 12,
    borderRadius: 12,
    marginBottom: 30,
  },
  searchInput: {
    flex: 1,
    marginLeft: 10,
    fontSize: 16,
    color: '#333333',
  },
  section: {
    paddingHorizontal: 20,
    marginBottom: 30,
  },
  sectionTitle: {
    fontSize: 20,
    fontWeight: '600',
    color: '#333333',
    marginBottom: 15,
  },
  horizontalScroll: {
    marginHorizontal: -20,
    paddingHorizontal: 20,
  },
  tripCard: {
    width: width * 0.4,
    marginRight: 15,
  },
  tripImage: {
    width: '100%',
    height: width * 0.3,
    borderRadius: 12,
    overflow: 'hidden',
    marginBottom: 8,
  },
  imagePlaceholder: {
    flex: 1,
    backgroundColor: '#87CEEB',
  },
  tripTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: '#333333',
    marginBottom: 4,
  },
  tripDetails: {
    fontSize: 14,
    color: '#8E8E8F',
  },
  suggestionCard: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#F0F9F9',
    marginHorizontal: 20,
    padding: 20,
    borderRadius: 12,
    marginBottom: 30,
  },
  suggestionIcon: {
    marginRight: 15,
  },
  suggestionText: {
    flex: 1,
  },
  suggestionTitle: {
    fontSize: 16,
    fontWeight: '500',
    color: '#333333',
  },
  suggestionSubtitle: {
    fontSize: 16,
    fontWeight: '500',
    color: '#333333',
  },
});
