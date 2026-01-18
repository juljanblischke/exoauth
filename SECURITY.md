# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.x.x   | :white_check_mark: |
| < 1.0   | :x:                |

## Reporting a Vulnerability

We take security vulnerabilities seriously. If you discover a security issue, please report it responsibly.

### How to Report

**Please DO NOT open a public GitHub issue for security vulnerabilities.**

Instead, please send an email to: **security@exoauth.com**

Include the following information:

- Type of vulnerability (e.g., XSS, SQL injection, authentication bypass)
- Full path to the affected source file(s)
- Step-by-step instructions to reproduce the issue
- Proof-of-concept or exploit code (if available)
- Impact assessment (what an attacker could achieve)

### What to Expect

1. **Acknowledgment**: We will acknowledge receipt within 48 hours
2. **Assessment**: We will investigate and assess the severity within 7 days
3. **Resolution**: We aim to release a fix within 30 days for critical issues
4. **Disclosure**: We will coordinate with you on public disclosure timing

### Severity Levels

| Level | Description | Response Time |
|-------|-------------|---------------|
| Critical | Authentication bypass, RCE, data breach | 24-48 hours |
| High | Privilege escalation, significant data exposure | 7 days |
| Medium | Limited data exposure, DoS | 30 days |
| Low | Minor issues, hardening | Next release |

## Security Best Practices

When deploying ExoAuth, please ensure:

### Infrastructure
- [ ] Use HTTPS/TLS 1.3 for all connections
- [ ] Keep all dependencies up to date
- [ ] Use strong, unique passwords for all services
- [ ] Enable firewall and restrict network access
- [ ] Use secrets management (not environment files in production)

### Configuration
- [ ] Change all default credentials
- [ ] Set strong JWT secrets (min 32 characters)
- [ ] Configure appropriate rate limits
- [ ] Enable audit logging
- [ ] Set up monitoring and alerting

### Database
- [ ] Use encrypted connections to PostgreSQL
- [ ] Enable encryption at rest
- [ ] Regular backups with encryption
- [ ] Restrict database user permissions

## Security Features

ExoAuth includes these security features:

- **Password Security**: Argon2 hashing with secure defaults
- **MFA**: TOTP with backup codes
- **Passkeys**: WebAuthn/FIDO2 support
- **Session Security**: Short-lived JWTs with refresh token rotation
- **Rate Limiting**: Configurable per-endpoint limits
- **Brute Force Protection**: Progressive delays and account lockout
- **Device Trust**: Fingerprinting and risk scoring
- **Audit Logging**: Comprehensive activity tracking
- **IP Restrictions**: Whitelist/blacklist support

## Acknowledgments

We appreciate responsible disclosure and will acknowledge security researchers who report valid vulnerabilities (with their permission).

---

Thank you for helping keep ExoAuth secure!
