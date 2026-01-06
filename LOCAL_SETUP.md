# Local Setup Guide

This guide will help you set up and run the Nimbbl Merchant Sample App on your local machine.

## Prerequisites

Before you begin, ensure you have the following installed:

- **[.NET 8.0 SDK](https://dotnet.microsoft.com/download)** or later
- A code editor (Visual Studio, VS Code, or Rider)
- Git (for cloning the repository)
- Nimbbl Access Key and Access Secret (from your [Nimbbl merchant dashboard](https://commandcenter.nimbbl.tech/))

## Step-by-Step Setup

### Step 1: Clone or Download the Repository

If you have the repository URL:
```bash
git clone <repository-url>
cd nimbbl-dotnet-sampleapp
```

Or if you already have the code, navigate to the `nimbbl-dotnet-sampleapp` directory:
```bash
cd nimbbl-dotnet-sampleapp
```

### Step 2: Install .NET 8.0 SDK

Verify you have .NET 8.0 installed:
```bash
dotnet --version
```

You should see `8.0.x` or higher. If not, download and install from [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download).

### Step 3: Configure Environment Variables

1. **Copy the example environment file:**
   ```bash
   cp env.example .env
   ```

2. **Edit the `.env` file** and add your Nimbbl credentials:
   ```bash
   NIMBBL_ACCESS_KEY=your_access_key_here
   NIMBBL_ACCESS_SECRET=your_access_secret_here
   ```

   **Where to get credentials:**
   - Log in to your [Nimbbl merchant dashboard](https://commandcenter.nimbbl.tech/)
   - Navigate to Developer Settings → Credentials
   - Copy your Access Key and Access Secret

### Step 4: Restore Dependencies

Restore all NuGet packages:
```bash
dotnet restore
```

This will download all required packages defined in the project files.

### Step 5: Build the Application

Build the project to ensure everything compiles correctly:
```bash
dotnet build
```

You should see:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Step 6: Run the Application

Start the application:
```bash
dotnet run
```

You should see output like:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://0.0.0.0:5001
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### Step 7: Access the Application

Open your web browser and navigate to:
```
http://localhost:5001
```

You should see the Nimbbl Payment Integration Demo page.

## Testing the Integration

### 1. Fill in the Payment Form

- **Amount:** Enter the payment amount (e.g., 4.00)
- **Currency:** Select INR, USD, or EUR
- **Checkout Mode:** Choose Popup or Redirect
- **User Details (Optional):** Fill in Name, Email, and Mobile if you want to prefill user information

### 2. Click "Pay Now"

- If you selected **Popup mode**, a payment popup will appear
- If you selected **Redirect mode**, you'll be redirected to the Nimbbl checkout page

### 3. Complete the Payment

- Follow the payment flow in the checkout
- After payment, you'll be redirected to the success or failure page

## Running on a Different Port

By default, the app runs on port 5001. To change the port:

1. Edit `Properties/launchSettings.json`
2. Change the `applicationUrl` value:
   ```json
   "applicationUrl": "http://0.0.0.0:5000"
   ```

Or set the port via environment variable:
```bash
export ASPNETCORE_URLS=http://localhost:5000
dotnet run
```

## Running on a Network IP Address

To access the app from other devices on your network:

1. The app is already configured to listen on `0.0.0.0:5001` in `launchSettings.json`
2. Find your machine's IP address:
   ```bash
   # macOS/Linux
   ifconfig | grep "inet "
   
   # Windows
   ipconfig
   ```
3. Access from another device using: `http://YOUR_IP_ADDRESS:5001`

## Troubleshooting

### Error: "NIMBBL_ACCESS_KEY environment variable is not set"

**Solution:** Make sure you've created a `.env` file and added your credentials. The app automatically loads environment variables from the `.env` file.

### Error: "Port 5001 is already in use"

**Solution:** Either:

**Option 1: Kill the process automatically (Recommended)**
- **macOS/Linux:** Run `./kill-ports-unix.sh`
- **Windows (PowerShell):** Run `.\kill-ports-windows.ps1`

**Option 2: Kill manually using commands**
- **macOS/Linux:**
  ```bash
  # Kill process on port 5001
  lsof -ti:5001 | xargs kill -9
  
  # Kill process on port 5000
  lsof -ti:5000 | xargs kill -9
  
  # Or kill both at once
  lsof -ti:5000,5001 | xargs kill -9
  ```
- **Windows (PowerShell):**
  ```powershell
  # Kill process on port 5001
  Get-NetTCPConnection -LocalPort 5001 | Select-Object -ExpandProperty OwningProcess | ForEach-Object { Stop-Process -Id $_ -Force }
  
  # Kill process on port 5000
  Get-NetTCPConnection -LocalPort 5000 | Select-Object -ExpandProperty OwningProcess | ForEach-Object { Stop-Process -Id $_ -Force }
  ```

**Option 3: Change the port**
- Change the port in `launchSettings.json`
- Set `ASPNETCORE_URLS` environment variable to a different port

### Error: "Build failed"

**Solution:**
1. Ensure you have .NET 8.0 SDK installed: `dotnet --version`
2. Restore packages: `dotnet restore`
3. Clean and rebuild: `dotnet clean && dotnet build`

### Application doesn't start

**Solution:**
1. Check that all environment variables are set correctly
2. Verify your Nimbbl credentials are valid
3. Check the console output for error messages
4. Ensure port 5001 (or your configured port) is not blocked by firewall

### Payment popup doesn't appear

**Solution:**
1. Check browser console for JavaScript errors (F12 → Console)
2. Ensure you're not blocking popups in your browser
3. Verify the order was created successfully (check server logs)
4. Check that `NIMBBL_API_HOST` is correct if using development environment

## Development Tips

### Hot Reload

Enable hot reload for faster development:
```bash
dotnet watch run
```

This will automatically restart the app when you make code changes.

### Debugging

1. **Visual Studio:** Press F5 to start debugging
2. **VS Code:** Press F5 and select ".NET Core" debugger
3. **Command Line:** Use `dotnet run` and attach a debugger

### Viewing Logs

The application logs are output to the console. For more detailed logging:

1. Set environment variable:
   ```bash
   NIMBBL_ENABLE_LOGGING=true
   NIMBBL_DEBUG_LOGGING=true
   ```

2. Check console output for detailed logs

## Project Structure

```
nimbbl-dotnet-sampleapp/
├── Controllers/          # MVC Controllers
│   └── HomeController.cs
├── Models/              # View Models
│   ├── IndexViewModel.cs
│   ├── PaymentSuccessViewModel.cs
│   └── PaymentFailedViewModel.cs
├── Views/               # Razor Views
│   └── Home/
├── NimbblCheckout/      # Checkout-related code
│   ├── CheckoutClient.cs
│   ├── CheckoutScriptBuilder.cs
│   └── PaymentResponseParser.cs
├── Services/            # Application services
│   ├── NimbblConfiguration.cs
│   └── EnvLoader.cs
├── Program.cs           # Application entry point
├── env.example          # Environment variables template (copy to .env)
└── README.md            # This file
```

## Next Steps

- Read [INTEGRATION.md](./INTEGRATION.md) for detailed integration guide
- Explore the code to understand how the SDK is used
- Customize the UI and payment flow for your needs
- Test different payment modes and scenarios

## Getting Help

If you encounter any issues:

- Check the [Troubleshooting](#troubleshooting) section above
- Review the [INTEGRATION.md](./INTEGRATION.md) guide
- Contact support:
  - Email: support@nimbbl.biz
  - Email: somya@nimbbl.biz

## Additional Resources

- [.NET Documentation](https://docs.microsoft.com/dotnet/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Nimbbl Dashboard](https://commandcenter.nimbbl.tech/)

