# Nimbbl Kit for ASP.NET Core MVC

Server kit for Nimbbl .NET Integration

## Requirements

- Latest [.NET 8.0 SDK](https://dotnet.microsoft.com/download) or later

## Quick Start

### 1. Configure Environment Variables

Copy the example environment file and fill in your credentials:

```bash
cp .env.example .env
```

Edit `.env` file and add your Nimbbl credentials:

```bash
NIMBBL_ACCESS_KEY=your_access_key_here
NIMBBL_ACCESS_SECRET=your_access_secret_here
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
3. Click "Pay Now" to test the checkout flow

## Configuration

### Required Environment Variables

- `NIMBBL_ACCESS_KEY` - Your Nimbbl access key (required)
- `NIMBBL_ACCESS_SECRET` - Your Nimbbl access secret (required)

## Documentation

- **For local setup and development:** See [LOCAL_SETUP.md](./LOCAL_SETUP.md) for detailed step-by-step instructions
- **For integration guide:** See [INTEGRATION.md](./INTEGRATION.md) for detailed integration documentation

## Support

For any assistance, you can reach us at:

- Email: support@nimbbl.biz