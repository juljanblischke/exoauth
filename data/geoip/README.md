# GeoIP Database

This directory should contain the MaxMind GeoLite2-City database for IP geolocation.

## Setup

1. Create a free account at https://www.maxmind.com/en/geolite2/signup
2. Download the GeoLite2-City database (MMDB format)
3. Place the `GeoLite2-City.mmdb` file in this directory

## Notes

- The database is optional - the application will work without it (location fields will be empty)
- The database is updated weekly by MaxMind
- Do NOT commit the .mmdb file to version control (it's in .gitignore)
