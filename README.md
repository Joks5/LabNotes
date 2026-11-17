## Project Structure

```
Lab-PC-Tracker/
├── assets/                  # Icons and images
├── database/
│   └── schema.sql           # Table definitions (optional, see earlier)
└── src/
    ├── main.cs              # Entry point and screen switching
    ├── constants.cs         # Shared colors and fonts
    ├── backend/
    │   ├── db.cs            # Opens the connection, enables foreign keys
    │   ├── auth.cs          # Login check, password hashing
    │   ├── computers.cs     # PC status and installed software queries
    │   ├── schedules.cs     # Bookings, conflict check
    │   ├── reports.cs       # Issue logs, alerts
    │   └── logic.cs         # Discrete math: set operations, truth tables
    └── frontend/
        ├── login/
        │   └── login.cs     # Login screen
        └── dashboard/
            ├── dashboard.cs        # Teacher main screen: schedule + lab dropdown
            ├── pc_detail.cs        # One PC's status and software
            ├── reports.cs          # Report form + logs table
            ├── admin_dashboard.cs  # Admin navigation
            └── admin_users.cs      # Create teacher accounts
```
