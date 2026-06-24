# 💻 SJ PC Store: Integrated Sales and Inventory Management System (SIMS)

## 📖 About the Project
The **Integrated Sales and Inventory Management System (SIMS)** is a centralized, Windows-based desktop application designed specifically for SJ PC Store, an award-winning local electronics business located along McArthur Highway in Bocaue, Philippines. Transitioning the shop from manual logbooks and fragmented Microsoft Excel sheets, this system streamlines daily operations by tightly integrating sales transactions with real-time, serialized inventory tracking. It reduces data entry errors, optimizes transaction speed, and provides accurate financial insights for management.

[![Watch the System Walkthrough](https://github.com/user-attachments/assets/6237eeeb-5035-4ecd-a559-480bd429d209)](https://github.com/user-attachments/assets/e3f759e3-4459-4351-8656-2347c4809cfc)






## 🔄 System Overview and Ecosystem
Unlike traditional retail environments with fixed manufacturer prices, SJ PC Store utilizes a component-based business model[cite: 1]. The system ecosystem addresses this by bridging two core enterprise workflows:

* **📦 Procure-to-Pay (P2P):** Handles inbound logistics. It categorizes wholesale or scrap hardware by specific blueprints (e.g., storage capacity and health, RAM generation) and logs the baseline costs, updating inventory and triggering low-stock alerts when inventory dips below minimum thresholds.
* **🛒 Order-to-Cash (O2C):** Manages outbound sales. Because the shop deals with second-hand units whose prices fluctuate based on their internal components, the system dynamically calculates the final price during the custom assembly and checkout process. It then processes the payment, issues a digital receipt, and finalizes the sale by deducting the specific serialized items from the inventory.

## ✨ System Features
* **🗄️ Two-Tier Inventory Management:** Separates the *Item Master* (hardware blueprints and baseline costs) from the *Stock Instance* (individual physical items tracked via unique alphanumeric serial numbers).
* **💰 Dynamic Pricing & Valuation Module:** Automatically aggregates the dynamically fluctuating second-hand values of internal components (CPU, RAM, SSD) to calculate the total selling price of custom-built desktop and laptop units.
* **🧾 Point-of-Sale (POS) Processing:** An automated checkout interface that processes customer transactions, calculates subtotals and discounts, and generates thermal-printed digital receipts containing specific warranty terms and serial numbers.
* **🚚 Procurement & Goods Receipt:** Allows staff to log Purchase Orders (POs)[cite: 1]. During physical delivery, the system dynamically generates input fields based on the PO quantity to enforce strict manual entry of unique serial numbers.
* **🔒 Role-Based Access Control & Security:** Restricts sensitive financial data to Administrators[cite: 1]. Features a secure offline account recovery protocol utilizing a customizable 6-character alphanumeric passkey.
* **♻️ Defective/Waste Tracking:** Isolates dead-on-arrival or defective components from sellable inventory while retaining their data for financial auditing.
* **📊 Report Generation Center:** Aggregates data to generate and export real-time PDF reports detailing daily, weekly, and monthly sales revenue, expenses, and inventory valuation.

## 🏗️ Technical Architecture
The application strictly adheres to the **Model-View-Controller (MVC)** software architecture pattern, cleanly decoupling the visual interface from the backend database logic to ensure code maintainability and scalability:
* **🧩 Model:** C# classes representing business entities (User, Product, Supplier, Transaction) and the ADO.NET logic responsible for executing CRUD operations with the Microsoft SQL Server database.
* **🖥️ View:** The visual layer built using Windows Forms (WinForms) responsible for data display and capturing user inputs.
* **⚙️ Controller:** The intermediary logic managers and C# event handlers that validate input, update the Model, and command the View to refresh.

## 🛠️ Tools and Technologies
* **Backend:** C# (.NET Framework)
* **Frontend:** Windows Forms (WinForms) customized with ReaLTaiizor and Scottplot 5 libraries for modern dashboard elements.
* **Database:** Microsoft SQL Server (SSMS)
* **Security:** BCrypt.Net-Next (secure cryptographic password hashing and salting)
* **Version Control:** Git & GitHub

## 🗺️ Development Roadmap
The project follows the Iterative Waterfall Software Development Life Cycle (SDLC) methodology to maintain structured rigor while allowing flexibility:

* **📝 Phase 1: Requirements Gathering and Analysis** 
  Identifying system requirements and business rules, focusing heavily on the unique component-based pricing logic and the transition from manual logbooks to a centralized digital database.
* **🎨 Phase 2: System Design** 
  Creating the technical blueprint, mapping out the Microsoft SQL Server database schemas, and designing the Windows Forms user interface layouts.
* **💻 Phase 3: Implementation** 
  Developing the software backend logic using C# and the MVC architecture, alongside constructing and integrating the relational database.
* **🕵️‍♂️ Phase 4: Testing** 
  Subjecting the system to rigorous testing to verify dynamic pricing accuracy, automatic stock deduction, and secure role-based access. 
* **🚀 Phase 5: Deployment** 
  Installing the fully tested system directly on the local computers at SJ PC Store, officially replacing manual tracking methods.
* **🔧 Phase 6: Maintenance** 
  Monitoring the system in real-world scenarios and actively collecting user feedback to address bottlenecks, which can trigger new iterative cycles for future software enhancements.

## 📂 Project Structure
```text
SJ-PC-Store-SIMS/
├── Controllers/         # Event handlers and logic managers bridging Views and Models
├── Models/              # C# entity classes and ADO.NET database connection logic
├── Views/               # WinForms UI interfaces (Login, Dashboard, POS, Inventory)
├── Database/            # SQL scripts for database schemas, tables, and stored procedures
├── Resources/           # Application assets, icons, and attachment file storage
├── Program.cs           # Main application entry point
└── App.config           # Local configuration and SQL Server connection strings
```

## ⚙️ Execution and Setup

**Prerequisites:**
* Visual Studio IDE (2019 or later)
* Microsoft SQL Server Express 2017
* SQL Server Management Studio (SSMS) 18.0

**Installation Steps:**

1.  **Clone the repository:**
```bash
    git clone [https://github.com/rewisPage/SJ-PC-Store-SIMS.git](https://github.com/rewisPage/SJ-PC-Store-SIMS.git)
```

2.  **Database Setup:** 
    * Open SSMS 18.0 and connect to your local SQL Server Express 2017 instance.
    * Locate the SQL script provided in the `Database/` folder of the cloned directory.
    * Execute the script to build the relational database, generating all necessary tables (e.g., `USER`, `ITEM_MASTER`, `STOCK_INSTANCE`, `TRANSACTION`).

3.  **Configure Connection:**
    * Open the solution in Visual Studio.
    * Navigate to the `App.config` file in the project explorer.
    * Update the connection string to match your local SQL Server instance name and authentication credentials.

4.  **Restore Packages & UI Libraries:**
    * Ensure all required NuGet packages are restored. Pay special attention to third-party UI libraries like `ReaLTaiizor` and `Siticone` so that the modern dashboard elements and animations render correctly, alongside `BCrypt.Net-Next` for security. 

5.  **Build and Run:**
    * Clean and compile the project, then run the application. 
    * *Note for initial testing:* You can use the default system administrator credentials to access the master dashboard:
      * **Username:** `admin`
      * **Password:** `adminPass123`
