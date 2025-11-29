# Frontend Multi-Selection Kullanım Kılavuzu

## ✅ Tamamlanan Değişiklikler

### 1. State Yapısı Güncellendi
Artık frontend tarafında tüm seçimler çoklu destekliyor:

```typescript
// ❌ ESKİ (Tek Seçim)
const [budget, setBudget] = useState('5000');
const [activityLevel, setActivityLevel] = useState<'Relaxed' | 'Moderate' | 'Active' | null>(null);
const [transport, setTransport] = useState<'Car' | 'Walking' | 'Public Transport' | null>(null);

// ✅ YENİ (Çoklu Seçim)
const [selectedBudgets, setSelectedBudgets] = useState<string[]>([]);
const [selectedIntensities, setSelectedIntensities] = useState<string[]>([]);
const [selectedTransports, setSelectedTransports] = useState<string[]>([]);
```

### 2. Enum Değerleri MULTI_SELECTION_GUIDE.md'ye Uygun Hale Getirildi

```typescript
// Themes (0-based indexing)
const themes = [
  { label: 'Nature', value: 0 },
  { label: 'Sea', value: 1 },
  { label: 'History', value: 2 },
  { label: 'Beach', value: 3 },
  { label: 'Food', value: 4 },
  { label: 'Photospot', value: 5 },
];

// Budgets
const budgets = [
  { label: 'Low', value: 0 },
  { label: 'Medium', value: 1 },
  { label: 'High', value: 2 },
];

// Intensities
const intensities = [
  { label: 'Relaxed', value: 0 },
  { label: 'Active', value: 1 },
];

// Transports
const transports = [
  { label: 'Car', value: 0 },
  { label: 'Walk', value: 1 },
  { label: 'Public Transport', value: 2 },
];
```

### 3. API Request Formatı Güncellendi

```typescript
// ❌ ESKİ FORMAT
const response = await api.post('/api/Routes/plan', {
  region: destination,
  days: Number.parseInt(days),
  theme: themeMap[selectedThemes[0]] || 1,
  budget: budgetLevel,
  intensity: intensityMap[activityLevel] || 1,
  transport: transportMap[transport] || 1,
});

// ✅ YENİ FORMAT (Doğru Parametre Sırası ile)
const requestBody = {
  region: destination,              // 1. parametre
  days: Number.parseInt(days),     // 2. parametre
  themes: themeValues,             // 3. parametre (array)
  budgets: budgetValues,           // 4. parametre (array)
  intensities: intensityValues,    // 5. parametre (array)
  transports: transportValues,     // 6. parametre (array)
};

const response = await api.post('/api/Routes/plan', requestBody);
```

### 4. UI Multi-Selection Desteği Eklendi

Artık tüm seçenekler (themes, budgets, intensities, transports) için çoklu seçim yapılabilir:

```typescript
// Toggle fonksiyonları
const toggleTheme = (theme: string) => { ... };
const toggleBudget = (budget: string) => { ... };
const toggleIntensity = (intensity: string) => { ... };
const toggleTransport = (transport: string) => { ... };
```

## 📋 Kullanım Örnekleri

### Örnek 1: Çeşitli Temalı Tur
```typescript
// Kullanıcı seçimleri:
- Themes: Nature, Sea, History
- Budget: Medium
- Intensity: Relaxed
- Transport: Walk, Public Transport

// Frontend'den gönderilecek:
{
  "region": "İzmir",
  "days": 3,
  "themes": [0, 1, 2],        // Nature, Sea, History
  "budgets": [1],             // Medium
  "intensities": [0],         // Relaxed
  "transports": [1, 2]        // Walk, Public Transport
}
```

### Örnek 2: Yemek ve Fotoğraf Odaklı
```typescript
// Kullanıcı seçimleri:
- Themes: Food, Photospot
- Budget: High
- Intensity: Active
- Transport: Car, Public Transport

// Frontend'den gönderilecek:
{
  "region": "Istanbul",
  "days": 2,
  "themes": [4, 5],          // Food, Photospot
  "budgets": [2],            // High
  "intensities": [1],        // Active
  "transports": [0, 2]       // Car, Public Transport
}
```

### Örnek 3: Karma Bütçe ve Yoğunluk
```typescript
// Kullanıcı seçimleri:
- Themes: Beach, Sea, Nature
- Budget: Medium, High
- Intensity: Relaxed, Active
- Transport: Car

// Frontend'den gönderilecek:
{
  "region": "Antalya",
  "days": 5,
  "themes": [3, 1, 0],       // Beach, Sea, Nature
  "budgets": [1, 2],         // Medium, High
  "intensities": [0, 1],     // Relaxed, Active
  "transports": [0]          // Car
}
```

## 🎨 UI Değişiklikleri

### Önceki UI:
- Budget: Slider (tek değer)
- Activity Level: 3 buton (tek seçim - Relaxed, Moderate, Active)
- Transport: 3 buton (tek seçim)

### Yeni UI:
- Budget: 3 chip (çoklu seçim - Low, Medium, High)
- Activity Level: 2 chip (çoklu seçim - Relaxed, Active)
- Transport: 3 chip (çoklu seçim - Car, Walk, Public Transport)

Tüm seçeneklerde "multiple allowed" etiketi gösterilir.

## 🔍 Validation

Artık her seçenek en az bir değer seçilmesini zorunlu kılar:

```typescript
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
```

## 🔄 Backend Uyumluluk

Backend zaten `RoutePlanRequestDto` ile hem eski single value hem de yeni multi-value formatını destekliyor. Frontend değişikliği sonrasında:

1. Frontend → Backend: **Yeni format** (arrays) kullanır
2. Backend → AI: **Tüm seçimler** gönderilir
3. Backend → Database: **Primary/ilk seçim** saklanır

## 📊 Parametre Sırası Önemi

AI'a gönderilen parametreler şu sıraya göre işlenir:

1. **region** - Hedef bölge
2. **days** - Gün sayısı
3. **themes** - Temalar (array)
4. **budgets** - Bütçe seviyeleri (array)
5. **intensities** - Aktivite yoğunluğu (array)
6. **transports** - Ulaşım modları (array)

Bu sıra hem frontend API isteğinde hem de backend'de tutarlı şekilde korunuyor.

## 🚀 Test Etme

Frontend'i test etmek için:

```bash
# Terminal 1: Backend'i çalıştır
cd backend/SmartTripApi
dotnet run

# Terminal 2: Frontend'i çalıştır
cd frontend
npm start
```

1. Uygulamayı aç
2. New Trip sekmesine git
3. Birden fazla theme, budget, intensity ve transport seç
4. "Plan Trip" butonuna tıkla
5. Console log'ları kontrol et:
   - `Sending request to backend:` mesajında array formatını göreceksin

## ✨ Önemli Notlar

- ✅ Backend değişikliğe gerek yok (zaten multi-selection destekliyor)
- ✅ Frontend artık multi-selection destekliyor
- ✅ Parametre sırası MULTI_SELECTION_GUIDE.md'ye uygun
- ✅ Enum değerleri doğru mapping edildi
- ✅ UI/UX güncellendi (slider kaldırıldı, chip'ler eklendi)

