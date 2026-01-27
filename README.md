# Nimbbl Kit for ASP.NET Core MVC

Server kit for Nimbbl .NET Integration

## Requirements

- Latest [.NET 8.0 SDK](https://dotnet.microsoft.com/download) or later
- [Nimbbl.Sdk.Rest](https://www.nuget.org/packages/Nimbbl.Sdk.Rest) (published NuGet package)

## Quick Start

### 1. Configure Environment Variables

Copy the example environment file and fill in your credentials:

```bash
cp env.example .env
```

Edit `.env` file and add your Nimbbl credentials:

```bash
NIMBBL_ACCESS_KEY=your_access_key_here
NIMBBL_ACCESS_SECRET=your_access_secret_here
NIMBBL_API_HOST=  # Optional: for different environments (default: api.nimbbl.tech)
ENCRYPT_PAYLOAD=false  # Optional: set to true to encrypt outgoing payloads
```

**Note:** Get your access key and secret from your [Nimbbl merchant dashboard](https://commandcenter.nimbbl.tech/). Navigate to Developer Settings → Credentials.

### 2. Build and Run

```bash
dotnet restore
dotnet build
dotnet run
```

The application will start at `http://localhost:5001` (or the port configured in `launchSettings.json`).

### 3. Test the Integration

1. Open your browser and navigate to the application URL
2. Fill in the payment form
3. Select checkout mode (Popup or Redirect)
4. Click "Pay Now" to test the checkout flow

**Checkout Modes:**
- **Popup Mode**: Opens checkout in a popup window, processes callback via POST `/payment-callback`
- **Redirect Mode**: Redirects to checkout page, processes callback via GET `/payment-callback?response=...`

**Payment Callbacks:**
- The sample app handles both GET and POST requests to `/payment-callback`
- Automatically parses and verifies signatures for both base64-encoded and JSON responses
- Extracts payment status from `transaction.status` (authoritative source)

## Configuration

### Required Environment Variables

- `NIMBBL_ACCESS_KEY` - Your Nimbbl access key (required)
- `NIMBBL_ACCESS_SECRET` - Your Nimbbl access secret (required)
- `NIMBBL_API_HOST` - Optional: API host URL for different environments. Defaults to `api.nimbbl.tech` if not set.
- `ENCRYPT_PAYLOAD` - Optional (default `false`): encrypt outgoing request payloads

**Note:** Incoming webhook/callback decryption is auto-detected (no env flag needed).

## SDK Reference (local vs NuGet)

By default, this sample app uses the published NuGet package.

If you prefer using the local SDK source instead, switch the reference in `nimbbl-dotnet-sampleapp.csproj` from `PackageReference` to `ProjectReference`.

## Documentation

- **For local setup and development:** See [LOCAL_SETUP.md](./LOCAL_SETUP.md) for detailed step-by-step instructions
- **For integration guide:** See [INTEGRATION.md](./INTEGRATION.md) for detailed integration documentation

## Support

For any assistance, you can reach us at:

- Email: support@nimbbl.biz