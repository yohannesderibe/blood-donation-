-- St. Amanuel Church Blood Donation System - PostgreSQL Schema

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Admins
CREATE TABLE admins (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(100) NOT NULL UNIQUE,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(200) NOT NULL,
    role VARCHAR(50) NOT NULL DEFAULT 'Admin',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Hospital Partners
CREATE TABLE hospital_partners (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL,
    contact_person VARCHAR(200),
    phone VARCHAR(50),
    email VARCHAR(255),
    notes TEXT,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Donors
CREATE TABLE donors (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    full_name VARCHAR(200) NOT NULL,
    christian_name VARCHAR(200) NOT NULL,
    phone VARCHAR(50) NOT NULL UNIQUE,
    email VARCHAR(255),
    blood_type VARCHAR(10) NOT NULL DEFAULT 'Unknown',
    gender VARCHAR(20),
    is_sunday_school_member BOOLEAN NOT NULL DEFAULT FALSE,
    password_hash VARCHAR(255) NOT NULL,
    is_first_time_donor BOOLEAN NOT NULL DEFAULT TRUE,
    last_donation_date DATE,
    previous_donation_count INT NOT NULL DEFAULT 0,
    how_heard_about_us VARCHAR(500),
    is_eligible BOOLEAN NOT NULL DEFAULT TRUE,
    eligibility_notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_donors_blood_type ON donors(blood_type);
CREATE INDEX idx_donors_is_eligible ON donors(is_eligible);
CREATE INDEX idx_donors_sunday_school ON donors(is_sunday_school_member);
CREATE INDEX idx_donors_last_donation ON donors(last_donation_date);
CREATE INDEX idx_donors_full_name ON donors(full_name);
CREATE INDEX idx_donors_phone ON donors(phone);

-- Donations
CREATE TABLE donations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    donor_id UUID NOT NULL REFERENCES donors(id) ON DELETE CASCADE,
    hospital_partner_id UUID REFERENCES hospital_partners(id) ON DELETE SET NULL,
    donation_date DATE NOT NULL DEFAULT CURRENT_DATE,
    verified_by VARCHAR(200),
    notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_donations_donor_id ON donations(donor_id);
CREATE INDEX idx_donations_date ON donations(donation_date);

-- SMS Logs
CREATE TABLE sms_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    admin_id UUID REFERENCES admins(id) ON DELETE SET NULL,
    recipient_type VARCHAR(50) NOT NULL,
    recipient_count INT NOT NULL DEFAULT 0,
    message_content TEXT NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    delivery_status TEXT,
    afro_message_id VARCHAR(255),
    cost_etb DECIMAL(10, 4),
    error_message TEXT,
    retry_count INT NOT NULL DEFAULT 0,
    sent_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_sms_logs_sent_at ON sms_logs(sent_at);

-- SMS Recipients (individual tracking)
CREATE TABLE sms_recipients (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    sms_log_id UUID NOT NULL REFERENCES sms_logs(id) ON DELETE CASCADE,
    donor_id UUID REFERENCES donors(id) ON DELETE SET NULL,
    phone VARCHAR(50) NOT NULL,
    delivery_status VARCHAR(50) DEFAULT 'Pending',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Audit Logs
CREATE TABLE audit_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    admin_id UUID REFERENCES admins(id) ON DELETE SET NULL,
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(100) NOT NULL,
    entity_id UUID,
    details TEXT,
    ip_address VARCHAR(50),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_audit_logs_created_at ON audit_logs(created_at);

-- Reports Metadata
CREATE TABLE reports_metadata (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    admin_id UUID REFERENCES admins(id) ON DELETE SET NULL,
    report_type VARCHAR(100) NOT NULL,
    file_name VARCHAR(255) NOT NULL,
    file_format VARCHAR(20) NOT NULL,
    date_from DATE,
    date_to DATE,
    record_count INT NOT NULL DEFAULT 0,
    generated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- System Notifications
CREATE TABLE system_notifications (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    title_en VARCHAR(255) NOT NULL,
    title_am VARCHAR(255),
    message_en TEXT NOT NULL,
    message_am TEXT,
    notification_type VARCHAR(50) NOT NULL DEFAULT 'Info',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    event_date DATE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Default admin is seeded by the .NET application (username: admin, password: Admin@123)

INSERT INTO system_notifications (title_en, title_am, message_en, message_am, notification_type, event_date)
VALUES
    ('Upcoming Blood Drive', 'የሚመጣ የደም ስጦታ', 'Annual blood donation drive scheduled for next month at St. Amanuel Church.', 'በሚቀጥለው ወር በቅዱስ አmanuel ቤተክርስቲያን የዓመታዊ የደም ስጦታ ይplanned ነው.', 'Drive', CURRENT_DATE + INTERVAL '30 days'),
    ('Hospital Request', 'የሆስፒታል ጥያቄ', 'Black Lion Hospital requests O+ donors urgently.', 'ብላክ ላዮን ሆስፒታል O+ ደም Contributors በአስቸኳይ ይፈልጋል.', 'Request', CURRENT_DATE + INTERVAL '7 days');
