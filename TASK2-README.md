1. /api/routes/plan (POST) - Ana Endpoint
İşlevi: Kullanıcıdan request alıp, AI'den plan oluşturup, DB'ye kaydedip, response döner.

POST /api/routes/plan
{
  "region": "Kaş",
  "days": 4,
  "theme": "nature",
  "budget": "medium",
  "intensity": "relaxed",
  "transport": "car"
}

İçerde ne olacak:
[HttpPost("plan")]
public async Task<ActionResult<TripPlanResponse>> CreateTripPlan(TripPlanRequest request)
{
    // 1. Request validation
    if (!ModelState.IsValid) return BadRequest();
    
    // 2. AI servisini çağır
    var aiResponse = await _aiService.GenerateTripPlanAsync(request);
    
    // 3. AI response'unu database'e kaydet
    var itineraryId = await _tripPlanningService.SaveToDatabase(aiResponse, request, userId);
    
    // 4. Frontend'e response dön (artık database ID'li) (sonraki aşamalarda burası)
    return Ok(new { 
        itineraryId = itineraryId,
        plan = aiResponse 
    });
}

2. /api/AI/generate-trip - Test Amaçlıdır.
İşlevi: Sadece AI'den response alır, DB'ye kaydetmez. Test için kullanılır.



TASK 2 — Rota Planlama Endpoint'i (POST /api/routes/plan)
🎯 Amaç
Frontend'in rota oluşturmak için çağıracağı API uç noktasını geliştirmek.

📌 Yapılacaklar (Detaylı)
Adım | Açıklama
-----|----------
Request modeli | TripPlanRequest.cs + validation attributes ([Required], [Range] vb.)
Response modeli | TripPlanResponse.cs oluşturulacak (AI'nin JSON çıktısı)
Controller | RoutesController.cs içinde /api/routes/plan POST yazılacak
Authentication | JWT token'dan user_id extract edilecek
Validasyon | Eksik parametre gelirse 400 hatası dönecek
AI çağrısı | Request → PromptBuilder → AIService → JSON dönüş
Error Handling | AI servis fail = 500, validation fail = 400
Response Format | Standardize edilmiş JSON response döndürecek
Save to DB çağrısı | Task 3'teki service metodunu çağıracak (preparasyon)

🔧 Örnek Response JSON
{
  "success": true,
  "itineraryId": 123,
  "message": "Trip plan created successfully",
  "data": {
    "planName": "...",
    "days": [...]
  }
}

✔ Teslim:
• Postman üzerinden plan isteği yapıldığında JSON rota dönmeli
• Hatalı input → açıklayıcı validation mesajı içermeli
• Authenticated user gerekli (JWT)
• Standart error response format