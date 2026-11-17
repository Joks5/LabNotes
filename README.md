## Project Structure

```
Lab-PC-Tracker/
├── assets/                  # Icons and images
├── database/
│   ├── labtracker.db        # The SQLite file
│   └── schema.sql           # Table definitions (optional, see earlier)
└── src/
    ├── main.py              # Entry point and screen switching
    ├── constants.py         # Shared colors and fonts
    ├── backend/
    │   ├── db.py            # Opens the connection, enables foreign keys
    │   ├── auth.py          # Login check, password hashing
    │   ├── computers.py     # PC status and installed software queries
    │   ├── schedules.py     # Bookings, conflict check
    │   ├── reports.py       # Issue logs, alerts
    │   └── logic.py         # Discrete math: set operations, truth tables
    └── frontend/
        ├── login/
        │   └── login.py     # Login screen
        └── dashboard/
            ├── dashboard.py        # Teacher main screen: schedule + lab dropdown
            ├── pc_detail.py        # One PC's status and software
            ├── reports.py          # Report form + logs table
            ├── admin_dashboard.py  # Admin navigation
            └── admin_users.py      # Create teacher accounts
```
