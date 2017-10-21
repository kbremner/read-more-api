ALTER TABLE "PocketAccounts"
ADD COLUMN EmailUserId UUID UNIQUE NOT NULL default gen_random_uuid();