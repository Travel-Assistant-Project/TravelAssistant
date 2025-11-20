import { StyleSheet, Text, View, TouchableOpacity, Dimensions, ImageBackground } from 'react-native';
import { useRouter } from 'expo-router';
import { StatusBar } from 'expo-status-bar';

const { width, height } = Dimensions.get('window');

export default function WelcomeScreen() {
  const router = useRouter();

  return (
    <View style={styles.container}>
      <StatusBar style="light" />
      
      {/* Background Gradient Effect */}
      <View style={styles.backgroundGradient}>
        
        {/* Main Content */}
        <View style={styles.contentContainer}>
          <View style={styles.textContainer}>
            <Text style={styles.appTitle}>AIventure</Text>
            <Text style={styles.subtitle}>Plan your{'\n'}trip smartly!</Text>
          </View>
          
          <TouchableOpacity 
            style={styles.getStartedButton}
            onPress={() => router.push('/(auth)/login')}
          >
            <Text style={styles.buttonText}>Get Started</Text>
          </TouchableOpacity>
        </View>
        
        {/* Bottom Globe Illustration */}
        <View style={styles.globeContainer}>
          <View style={styles.globe} />
        </View>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  backgroundGradient: {
    flex: 1,
    backgroundColor: '#5C9B9B', // Teal color from the design
    justifyContent: 'space-between',
    paddingTop: 80,
    paddingBottom: 0,
  },
  contentContainer: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 40,
  },
  textContainer: {
    alignItems: 'center',
    marginBottom: 60,
  },
  appTitle: {
    fontSize: 36,
    fontWeight: 'bold',
    color: 'white',
    textAlign: 'center',
    marginBottom: 20,
  },
  subtitle: {
    fontSize: 28,
    fontWeight: '600',
    color: 'white',
    textAlign: 'center',
    lineHeight: 34,
  },
  getStartedButton: {
    backgroundColor: 'rgba(255, 255, 255, 0.9)',
    paddingVertical: 16,
    paddingHorizontal: 48,
    borderRadius: 30,
    elevation: 3,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.25,
    shadowRadius: 3.84,
  },
  buttonText: {
    fontSize: 18,
    fontWeight: '600',
    color: '#5C9B9B',
    textAlign: 'center',
  },
  globeContainer: {
    alignItems: 'center',
    marginTop: 'auto',
  },
  globe: {
    width: width * 0.8,
    height: width * 0.4,
    backgroundColor: 'rgba(255, 255, 255, 0.2)',
    borderRadius: width * 0.4,
    marginBottom: -width * 0.2, // Partially hidden at bottom
  },
});
