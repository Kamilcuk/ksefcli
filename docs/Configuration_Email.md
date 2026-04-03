# Email Configuration

To use direct email sending (without relying on local `mailx`), add an `smtp` section to your `kcksefcli.yaml` at the root level:

```yaml
smtp:
  host: smtp.gmail.com
  port: 587
  user: user@gmail.com
  password_env: SMTP_PASSWORD # Or just 'password: your_password'
  from: "KSeF Notifier <user@gmail.com>"
  use_ssl: true

active_profile: my_profile
profiles:
  my_profile:
    ...
```

## Options

- `host`: SMTP server hostname.
- `port`: SMTP server port (usually 587 for STARTTLS or 465 for SSL).
- `user`: Username for authentication.
- `password`: Password for authentication (literal string).
- `password_env`: Name of the environment variable containing the password.
- `from`: The sender email address.
- `use_ssl`: Whether to use SSL/TLS. If port is 465, SSL is used automatically.
