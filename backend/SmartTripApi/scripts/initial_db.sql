CREATE TYPE budget_level AS ENUM ('low', 'medium', 'high');
CREATE TYPE intensity_level AS ENUM ('relaxed', 'active');
CREATE TYPE transport_mode AS ENUM ('car', 'walk', 'public_transport');
CREATE TYPE theme_type AS ENUM ('nature', 'sea', 'history', 'beach', 'food', 'photospot');
CREATE TYPE user_role AS ENUM ('user', 'admin');


-- =============================
-- USERS
-- =============================
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(150) UNIQUE NOT NULL,
    password VARCHAR(255) NOT NULL,
    age INT,
    country VARCHAR(100),
    city VARCHAR(100),
    role user_role DEFAULT 'user',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- =============================
-- ITINERARIES (Rotalar)
-- =============================
CREATE TABLE itineraries (
    id SERIAL PRIMARY KEY,
    user_id INT REFERENCES users(id) ON DELETE CASCADE,
    name VARCHAR(150) NOT NULL,              -- Örn: Kaş Tatilimiz
    region VARCHAR(150) NOT NULL,            -- Örn: Fethiye / Kaş
    days_count INT NOT NULL,                 -- Örn: 5 (4 gece 5 gün)
    theme theme_type,
    budget budget_level,
    intensity intensity_level,
    transport transport_mode,
    is_ai_generated BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- =============================
-- ITINERARY_DAYS (Rota Günleri)
-- =============================
CREATE TABLE itinerary_days (
    id SERIAL PRIMARY KEY,
    itinerary_id INT NOT NULL REFERENCES itineraries(id) ON DELETE CASCADE,
    day_number INT NOT NULL,
    weather_info JSONB,                      -- OpenWeather API verisi
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (itinerary_id, day_number)
);

-- =============================
-- PLACES (Mekanlar)
-- =============================
CREATE TABLE places (
    id SERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    category theme_type,                     -- Örn: doğa, tarih, plaj, vs.
    latitude DECIMAL(10,8),
    longitude DECIMAL(11,8),
    city VARCHAR(100),
    country VARCHAR(100),
    google_maps_url TEXT,
    google_rating DECIMAL(2,1),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- =============================
-- ACTIVITIES (Aktiviteler)
-- =============================
CREATE TABLE activities (
    id SERIAL PRIMARY KEY,
    itinerary_day_id INT REFERENCES itinerary_days(id) ON DELETE CASCADE,
    place_id INT REFERENCES places(id) ON DELETE SET NULL,
    title VARCHAR(200) NOT NULL,
    description TEXT,
    reason TEXT,                             -- "neden önerildi" açıklaması
    start_time TIME,
    end_time TIME,
    image_urls TEXT[],                       -- Google Places API'den alınan görseller
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- =============================
-- PLACE_PHOTOS (Mekan Görselleri)
-- =============================
CREATE TABLE place_photos (
    id SERIAL PRIMARY KEY,
    place_id INT REFERENCES places(id) ON DELETE CASCADE,
    image_url TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- =============================
-- FAVORITES (Favoriler)
-- =============================
CREATE TABLE favorites (
    id SERIAL PRIMARY KEY,
    user_id INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    place_id INT REFERENCES places(id) ON DELETE CASCADE,
    itinerary_id INT REFERENCES itineraries(id) ON DELETE CASCADE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CHECK (
        (place_id IS NOT NULL AND itinerary_id IS NULL)
        OR
        (place_id IS NULL AND itinerary_id IS NOT NULL)
    )
);

-- =============================
-- AI_REQUESTS (AI API log)
-- =============================
CREATE TABLE ai_requests (
    id SERIAL PRIMARY KEY,
    user_id INT REFERENCES users(id) ON DELETE CASCADE,
    itinerary_id INT REFERENCES itineraries(id) ON DELETE CASCADE,
    request_payload JSONB NOT NULL,          -- Kullanıcının gönderdiği filtreler (tema, gün, bütçe, vs.)
    ai_response JSONB,                       -- AI’den dönen rota planı
    status VARCHAR(50) DEFAULT 'completed',  -- completed / failed / pending
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- =============================
-- GENERAL COMMENTS / REVIEWS (Opsiyonel)
-- =============================
CREATE TABLE reviews (
    id SERIAL PRIMARY KEY,
    user_id INT REFERENCES users(id) ON DELETE CASCADE,
    place_id INT REFERENCES places(id) ON DELETE CASCADE,
    comment TEXT,
    rating INT CHECK (rating BETWEEN 1 AND 5),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- =============================
-- INDEXES (Performans için)
-- =============================
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_itineraries_user ON itineraries(user_id);
CREATE INDEX idx_places_category ON places(category);
CREATE INDEX idx_favorites_user ON favorites(user_id);
CREATE INDEX idx_activities_itinerary_day ON activities(itinerary_day_id);
CREATE INDEX idx_reviews_place ON reviews(place_id);
