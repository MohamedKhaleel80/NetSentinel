# Backend Security Configuration

The backend no longer reads JWT or Snort credentials from tracked JSON settings. Configure them in process environment variables or a supported secret store before starting the API:

- `JwtSettings__SecretKey`: unique high-entropy signing key (at least 32 random bytes).
- `SnortSettings__ApiKey`: high-entropy key shared with the Kali relay through its protected environment file.
- `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, ...: exact browser origins, for example `http://localhost:8080` and the actual approved Flutter web origin on the LAN.

For PowerShell, set the environment variables in the same process that launches `dotnet run`. Replace the credential placeholders with values delivered through an approved secret-management channel; never commit them or paste them into logs. The API fails at startup if either credential is missing or if the CORS origin list is empty or malformed.

Changing `JwtSettings__SecretKey` invalidates existing access and refresh-token signatures; plan user reauthentication. Changing `SnortSettings__ApiKey` requires coordinating the same replacement with the relay before restarting either component. The currently running backend retains its loaded configuration until it is deliberately restarted.

The JSON CORS allowlist is intentionally limited to local Flutter development origins. For a LAN deployment, replace or override it with the exact origin from which the browser loads the Flutter app. Do not use `*`; CORS is not a substitute for API authentication.