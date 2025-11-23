# Multi-Selection Route Planning Guide

## Overview
Rota planlama API'sı artık birden fazla tema, budget, intensity ve transport modu seçmeyi destekliyor!

## API Changes

### Before (Old Format - Single Selection)
```json
{
  "region": "İzmir",
  "days": 1,
  "theme": 2,
  "budget": 1,
  "intensity": 1,
  "transport": 1
}
```

### After (New Format - Multiple Selections)
```json
{
  "region": "İzmir",
  "days": 1,
  "themes": [0, 1, 2],      // nature, sea, history
  "budgets": [1],           // medium
  "intensities": [0, 1],    // relaxed ve active karışık
  "transports": [1, 2]      // walk ve public_transport
}
```

## Enum Values (0-based indexing)

### Themes
- `0` = Nature (doğa)
- `1` = Sea (deniz)
- `2` = History (tarih)
- `3` = Beach (plaj)
- `4` = Food (yemek)
- `5` = Photospot (fotoğraf noktaları)

### Budgets
- `0` = Low (düşük)
- `1` = Medium (orta)
- `2` = High (yüksek)

### Intensities
- `0` = Relaxed (rahat)
- `1` = Active (aktif)

### Transports
- `0` = Car (araba)
- `1` = Walk (yürüyüş)
- `2` = Public Transport (toplu taşıma)

## Example Use Cases

### 1. Çeşitli Temalı Tur
Hem doğa, hem tarih, hem de deniz aktiviteleri içeren bir tur:
```json
{
  "region": "İzmir",
  "days": 3,
  "themes": [0, 1, 2],      // nature, sea, history
  "budgets": [1],           // medium
  "intensities": [0],       // relaxed
  "transports": [1, 2]      // walk and public transport
}
```

AI, 3 günlük tur boyunca:
- Bazı günler doğa aktiviteleri
- Bazı günler plaj/deniz aktiviteleri
- Bazı günler tarihi yerler
içeren dengeli bir plan oluşturacak.

### 2. Sadece Yemek ve Fotoğraf Odaklı
```json
{
  "region": "İstanbul",
  "days": 2,
  "themes": [4, 5],         // food, photospot
  "budgets": [2],           // high
  "intensities": [1],       // active
  "transports": [0, 2]      // car and public transport
}
```

### 3. Karma Bütçe ve Yoğunluk
Bazı günler rahat, bazı günler aktif geçsin:
```json
{
  "region": "Antalya",
  "days": 5,
  "themes": [3, 1, 0],      // beach, sea, nature
  "budgets": [1, 2],        // medium ve high arası
  "intensities": [0, 1],    // hem rahat hem aktif günler
  "transports": [0]         // sadece araba
}
```

### 4. Tek Tema (Eski Sistemle Uyumlu)
Tek tema seçmek de mümkün:
```json
{
  "region": "Kapadokya",
  "days": 2,
  "themes": [2],            // sadece history
  "budgets": [0],           // low
  "intensities": [0],       // relaxed
  "transports": [1]         // walk
}
```

## Implementation Details

### Backend Changes

1. **DTO Changes** (`RoutePlanDto.cs`):
   - Properties changed from `int` to `List<int>`
   - Added helper methods to convert lists to enums
   - Added `GetPrimary*()` methods for database storage

2. **Controller Changes** (`RoutesController.cs`):
   - Uses primary (first) selection for database storage
   - Passes all selections to AI service

3. **AI Prompt Changes** (`PromptBuilder.cs`):
   - Prompt now includes all selected themes
   - AI instructed to diversify activities across themes
   - Better variety in the generated itinerary

### Database Storage
Veritabanında hala sadece **primary/first selection** saklanıyor (eski tablo yapısını korumak için):
- `theme` → themes dizisinin ilk elemanı
- `budget` → budgets dizisinin ilk elemanı
- `intensity` → intensities dizisinin ilk elemanı
- `transport` → transports dizisinin ilk elemanı

Ama AI'a **tüm seçimler** gönderiliyor, böylece daha çeşitli aktiviteler oluşturuluyor.

## Frontend Integration

React Native/Expo tarafında şu şekilde kullanabilirsiniz:

```typescript
// Example: Multi-select form state
const [selectedThemes, setSelectedThemes] = useState<number[]>([]);
const [selectedBudgets, setSelectedBudgets] = useState<number[]>([]);
const [selectedIntensities, setSelectedIntensities] = useState<number[]>([]);
const [selectedTransports, setSelectedTransports] = useState<number[]>([]);

// API Request
const createRoutePlan = async () => {
  const response = await fetch('http://localhost:5000/api/routes/plan', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      region: "İzmir",
      days: 3,
      themes: selectedThemes,        // [0, 1, 2]
      budgets: selectedBudgets,      // [1]
      intensities: selectedIntensities,  // [0, 1]
      transports: selectedTransports     // [1, 2]
    })
  });
  
  const data = await response.json();
  return data;
};
```

## Testing

Test etmek için:

```bash
curl -X POST http://localhost:5000/api/routes/plan \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "region": "İzmir",
    "days": 2,
    "themes": [0, 1, 2],
    "budgets": [1],
    "intensities": [0, 1],
    "transports": [1, 2]
  }'
```

## Notes

- Boş array gönderirseniz, default değerler kullanılır
- AI, birden fazla tema seçildiğinde aktiviteleri dengeli dağıtacak şekilde eğitildi
- Database'de sadece primary selection saklanıyor ama AI'a hepsi gönderiliyor
- Frontend'de multi-select checkbox/chip component kullanabilirsiniz
