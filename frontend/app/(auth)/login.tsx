import { StyleSheet, Text, View, TouchableOpacity, TextInput, Dimensions, Alert } from 'react-native';
import { useRouter } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { useState } from 'react';

const { width } = Dimensions.get('window');

export default function LoginScreen() {
  const router = useRouter();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  const handleLogin = () => {
    if (!email || !password) {
      Alert.alert('Error', 'Please fill in all fields');
      return;
    }
    
    // TODO: Add actual login logic here
    console.log('Login attempt:', { email, password });
    
    // For now, navigate to tabs on any login attempt
    router.replace('/(tabs)');
  };

  const goToRegister = () => {
    router.push('./register');
  };

  return (
    <View style={styles.container}>
      <StatusBar style="dark" />
      
      <View style={styles.content}>
        <View style={styles.header}>
          <Text style={styles.title}>Welcome Back!</Text>
          <Text style={styles.subtitle}>Sign in to continue your journey</Text>
        </View>

        <View style={styles.form}>
          <View style={styles.inputContainer}>
            <TextInput
              style={styles.input}
              placeholder="Email"
              value={email}
              onChangeText={setEmail}
              keyboardType="email-address"
              autoCapitalize="none"
              autoCorrect={false}
            />
          </View>

          <View style={styles.inputContainer}>
            <TextInput
              style={styles.input}
              placeholder="Password"
              value={password}
              onChangeText={setPassword}
              secureTextEntry
              autoCapitalize="none"
            />
          </View>

          <TouchableOpacity style={styles.loginButton} onPress={handleLogin}>
            <Text style={styles.loginButtonText}>Login</Text>
          </TouchableOpacity>

          <TouchableOpacity style={styles.forgotPassword}>
            <Text style={styles.forgotPasswordText}>Forgot Password?</Text>
          </TouchableOpacity>
        </View>

        <View style={styles.footer}>
          <Text style={styles.footerText}>Don't have an account? </Text>
          <TouchableOpacity onPress={goToRegister}>
            <Text style={styles.signUpText}>Sign Up</Text>
          </TouchableOpacity>
        </View>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#FFFFFF',
  },
  content: {
    flex: 1,
    paddingHorizontal: 30,
    justifyContent: 'center',
  },
  header: {
    alignItems: 'center',
    marginBottom: 50,
  },
  title: {
    fontSize: 32,
    fontWeight: 'bold',
    color: '#333333',
    marginBottom: 10,
  },
  subtitle: {
    fontSize: 16,
    color: '#666666',
    textAlign: 'center',
  },
  form: {
    marginBottom: 30,
  },
  inputContainer: {
    marginBottom: 20,
  },
  input: {
    backgroundColor: '#F8F9FA',
    paddingHorizontal: 20,
    paddingVertical: 15,
    borderRadius: 12,
    fontSize: 16,
    borderWidth: 1,
    borderColor: '#E9ECEF',
  },
  loginButton: {
    backgroundColor: '#5C9B9B',
    paddingVertical: 16,
    borderRadius: 12,
    marginTop: 20,
    elevation: 2,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 3.84,
  },
  loginButtonText: {
    color: 'white',
    fontSize: 18,
    fontWeight: '600',
    textAlign: 'center',
  },
  forgotPassword: {
    alignItems: 'center',
    marginTop: 20,
  },
  forgotPasswordText: {
    color: '#5C9B9B',
    fontSize: 16,
  },
  footer: {
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
  },
  footerText: {
    fontSize: 16,
    color: '#666666',
  },
  signUpText: {
    fontSize: 16,
    color: '#5C9B9B',
    fontWeight: '600',
  },
});
