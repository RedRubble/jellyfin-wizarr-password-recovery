[![Add to Jellyfin](https://img.shields.io/badge/Add%20to%20Jellyfin-Plugin%20Repository-blue?logo=jellyfin&style=for-the-badge)](https://raw.githubusercontent.com/StunBeta/jellyfin-wizarr-password-recovery/master/manifest.json)

# Wizarr Password Recovery — Jellyfin Plugin

A Jellyfin plugin that replaces the native PIN‑based password reset system with a secure email‑based workflow powered by **Wizarr**.

This plugin watches Jellyfin’s internal password reset events, generates a secure Wizarr reset link, and sends it to the user via SMTP.

---

## ✨ Features

- Fully replaces Jellyfin’s built‑in password reset flow  
- Generates secure reset links using **Wizarr**  
- Sends reset emails via SMTP  
- Anti‑spam throttling per user  
- Independent test buttons (Wizarr + SMTP)  
- Customizable email subject & body  
- Works on any Jellyfin server (Docker, bare metal, Windows, Linux)

---

## 📦 Installation

1. Open **Jellyfin Dashboard**
2. Go to **Plugins → Repositories**
3. Click **Add Repository**
4. Enter:

| Setting | Value |
|---------|-------|
| Name | Wizarr Password Recovery |
| URL | https://raw.githubusercontent.com/StunBeta/jellyfin-wizarr-password-recovery/master/manifest.json |

> **Note:** Replace `StunBeta/jellyfin-wizarr-password-recovery` with your repository if you fork this project.

5. Save and restart Jellyfin

Go to **Plugins → Catalog**

Install **Wizarr Password Recovery**

---

## 🔧 How it works

1. User triggers **“Forgot Password”** in Jellyfin.  
2. Jellyfin writes a `passwordreset*.json` file in its **ProgramData** directory.  
3. The plugin’s file watcher detects the file creation and reads:
   - `UserName`
   - expiration timestamp  
4. The plugin communicates with Wizarr:
   - `GET /users` → find the user’s email by username  
   - `POST /users/{user_id}/reset-password` → generate a secure reset link  
5. The plugin sends the reset email via SMTP using your configured template.

This completely replaces Jellyfin’s PIN‑based reset mechanism.

---

## ⚙️ Required setup (Plugin configuration page)

### 🔗 Wizarr
- **WizarrBaseUrl** (example: `http://wizarr:5690`)  
- **WizarrApiKey**

### 📧 SMTP
- **FromEmail**  
- **FromName** (optional)  
- **SmtpHost**  
- **SmtpPort**  
- **SmtpUseSsl**  
- **SmtpUsername** / **SmtpPassword** (if required)  
- **TestEmailTo** (optional; used by the SMTP test button)

### ✉️ Email customization
- **EmailSubject**  
- **EmailBodyTemplate**  
  - Supports variables:
    - `{username}`
    - `{reset_link}`

### 🛡️ Anti‑spam
- **MinMinutesBetweenEmailsPerUser**  
  - Prevents repeated reset emails within a short time window.

---

## 🧪 Independent tests

From the plugin configuration page, you can run:

### **Test Wizarr Connection**  
Validates API key, base URL, and connectivity.

### **Test SMTP**  
Sends a test email using your SMTP settings.

---

## 📄 License

This project is licensed under:

**Creative Commons Attribution‑NonCommercial 4.0 International (CC BY‑NC 4.0)**  
Free for personal use — **commercial use requires a paid license**.

You are free to:

Share: copy and redistribute the material in any medium or format

Adapt: remix, transform, and build upon the material

Under the following terms:

Attribution: You must give appropriate credit.

NonCommercial: You may not use the material for commercial purposes.

Full license text:
https://creativecommons.org/licenses/by-nc/4.0/legalcode


---

## ❤️ Support

If you enjoy this plugin or want to support future development, feel free to star the repository or contribute.
