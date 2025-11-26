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


