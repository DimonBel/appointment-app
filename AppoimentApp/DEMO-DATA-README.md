# Quick Start Guide - Appointment App with Demo Data

## ✅ Setup Complete!

Demo data has been added to display appointments in the UI. Follow these simple steps:

## 🎯 Option 1: Start Frontend Only (Recommended - Uses Mock Data)

The frontend now includes mock demo data for quick testing:

```bash
cd appointmentapp-frontend-react
npm install  # Only needed first time
npm run dev
```

Then open: http://localhost:5173

**Demo Credentials:**
- Role: Patient
- Email: Any email
- Password: Any password

You'll see 16 pre-loaded appointments!

## 🎯 Option 2: Start with Backend APIs

If you want to use the actual backend:

### 1. Start Backend API with Demo Data:

```bash
cd /home/dumas/Desktop/Final-proj/appointment-app/AppoimentApp
./add-demo-data.sh
```

This will:
- ✅ Add 16 demo appointments
- ✅ With 8 different doctors
- ✅ Various consultation fees ($100-$200)
- ✅ Appointments spread over next 7 days

### 2. Start Other Backend Services (Optional):

```bash
# Terminal 2 - Appointment Booking
cd AppointmentBooking.Presention
dotnet run

# Terminal 3 - Management API
cd DoctorAppointmentManagement.Presention
dotnet run
```

### 3. Start Frontend:

```bash
# Terminal 4 - Frontend
cd appointmentapp-frontend-react
npm run dev
```

## 📊 View Demo Data

### API Endpoints:
- All Slots: http://localhost:5112/api/DoctorSlot/all
- Available Only: http://localhost:5112/api/DoctorSlot/available
- Swagger UI: http://localhost:5112/swagger

### Sample Doctors in Demo Data:
- Dr. Sarah Johnson - $150
- Dr. Michael Chen - $125
- Dr. Emily Rodriguez - $100
- Dr. James Williams - $200
- Dr. Olivia Martinez - $175
- Dr. David Kim - $150
- Dr. Jessica Taylor - $125
- Dr. Robert Anderson - $100

## 🎨 UI Features

The frontend now includes:
- ✨ **Premium glassmorphism design**
- 💎 **3D card effects and animations**
- 🌈 **Gradient backgrounds with floating elements**
- 🎯 **Interactive hover states**
- 💫 **Smooth transitions and ripple effects**
- 🔥 **Modern badge systems**
- 🌟 **Frosted glass modals**

## 🔄 Reset Demo Data

To add fresh demo data:

```bash
# Simply run the script again:
./add-demo-data.sh
```

## 📝 Notes

- The frontend works with or without backend (uses mock data as fallback)
- All appointments are set for future dates (Feb 13-21, 2026)
- Each doctor has multiple time slots
- All slots are initially marked as "Available"

Enjoy your beautiful appointment booking app! 🎉
