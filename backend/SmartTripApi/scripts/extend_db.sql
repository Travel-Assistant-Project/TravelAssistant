---- 23.11.2025
ALTER TABLE places
ADD COLUMN IF NOT EXISTS google_place_id VARCHAR(255),
ADD COLUMN IF NOT EXISTS formatted_address TEXT,
ADD COLUMN IF NOT EXISTS user_ratings_total INT,
ADD COLUMN IF NOT EXISTS price_level INT,
ADD COLUMN IF NOT EXISTS opening_hours JSONB,
ADD COLUMN IF NOT EXISTS photo_urls TEXT[];

-- Add status column to itineraries table
ALTER TABLE itineraries
ADD COLUMN IF NOT EXISTS status VARCHAR(50) DEFAULT 'pending';

-- Update existing records to 'completed' if they were created successfully
UPDATE itineraries
SET status = 'completed'
WHERE status IS NULL;


-- 23.11.2025

CREATE TABLE IF NOT EXISTS google_reviews (
    id SERIAL PRIMARY KEY,
    place_id INT NOT NULL REFERENCES places(id) ON DELETE CASCADE,
    author_name VARCHAR(255) NOT NULL,
    comment TEXT NOT NULL,
    rating INT NOT NULL CHECK (rating BETWEEN 1 AND 5),
    profile_photo_url TEXT,
    review_time TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for better query performance
CREATE INDEX IF NOT EXISTS idx_google_reviews_place ON google_reviews(place_id);
CREATE INDEX IF NOT EXISTS idx_google_reviews_created_at ON google_reviews(created_at);
CREATE INDEX IF NOT EXISTS idx_google_reviews_rating ON google_reviews(rating);


--30.11.2025
-- Add transport columns to activities table
ALTER TABLE activities
ADD COLUMN IF NOT EXISTS travel_from_previous_mode VARCHAR(50),
ADD COLUMN IF NOT EXISTS travel_from_previous_duration_minutes INT,
ADD COLUMN IF NOT EXISTS travel_from_previous_distance_meters INT,
ADD COLUMN IF NOT EXISTS travel_from_previous_details_json JSONB;

ALTER TABLE activities
ADD COLUMN IF NOT EXISTS travel_from_previous_polyline TEXT;

-- Create new table for detailed transport info
CREATE TABLE IF NOT EXISTS activity_transports (
    id SERIAL PRIMARY KEY,
    activity_id INT NOT NULL REFERENCES activities(id) ON DELETE CASCADE,
    
    -- Transport Mode (walking, driving, transit)
    mode VARCHAR(50) NOT NULL,
    
    -- Summary Data
    duration_minutes INT,
    distance_meters INT,
    
    -- Visualization Data
    polyline_points TEXT, -- For drawing route on map
    
    -- Detailed Steps (JSONB for storing complex step data)
    -- Contains: instructions, transit_lines, departure_times, etc.
    details JSONB,
    
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Create index for faster lookups
CREATE INDEX IF NOT EXISTS idx_activity_transports_activity_id ON activity_transports(activity_id);

----- 28.12.2025
-- places tablosuna photo_references kolonu ekle
-- ALTER TABLE places 
-- ADD COLUMN photo_references text[] NULL;

-- COMMENT ON COLUMN places.photo_references IS 'Google Places photo reference strings for proxy usage';

---- 09.01.2026
CREATE TABLE IF NOT EXISTS itinerary_request_indexes (
    id SERIAL PRIMARY KEY,
    user_id INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    itinerary_id INT NOT NULL REFERENCES itineraries(id) ON DELETE CASCADE,
    request_hash TEXT NOT NULL,
    created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_itinerary_request_indexes_user_hash
ON itinerary_request_indexes (user_id, request_hash);


----- 11.01.2026
-- Add SelectedTransportModes column to Itineraries table
ALTER TABLE "Itineraries" 
ADD COLUMN "SelectedTransportModes" TEXT;