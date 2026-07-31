# Database-first setup guide — StartupIMS backend

This walks you through everything from "I have MySQL and Visual Studio" to
"my API is running and reading from tables I created by hand." Follow it in
order — don't skip steps even if they seem obvious.

---

## Part 1 — Make sure MySQL is installed and running

1. If you don't already have MySQL installed, download **MySQL Community Server**
   from https://dev.mysql.com/downloads/mysql/ and install it with default options.
   During setup it will ask you to set a **root password** — remember this, you'll
   need it later.
2. Also install **MySQL Workbench** (same download page, or a separate installer at
   https://dev.mysql.com/downloads/workbench/) — this gives you a visual tool to
   run SQL scripts and browse tables, instead of using a command line.
3. Open **MySQL Workbench**. On the home screen, click the **+** icon next to
   "MySQL Connections" to create a new connection:
   - Connection Name: `localhost`
   - Hostname: `127.0.0.1`
   - Port: `3306`
   - Username: `root`
   - Click **Test Connection**, enter your root password, and confirm it succeeds.
4. Double-click that connection to open it. You should see a query editor.

---

## Part 2 — Run the SQL scripts to create your databases and tables

1. In the zip I gave you, find the `database` folder — it has two files:
   `01_identity_schema.sql` and `02_core_schema.sql`.
2. In MySQL Workbench, go to **File → Open SQL Script...** and select
   `01_identity_schema.sql`.
3. Click the **lightning bolt icon** (Execute) in the toolbar, or press `Ctrl+Shift+Enter`,
   to run the whole script.
4. Check the **Output** panel at the bottom — it should show green checkmarks with
   no red error rows. This created a database called `startupims_identity` with
   two tables: `Users` and `RefreshTokens`.
5. Repeat: **File → Open SQL Script...** → select `02_core_schema.sql` → execute it.
   This creates `startupims_core` with six tables (`Startups`, `Mentors`, etc.)
6. On the left sidebar, click the refresh icon under "SCHEMAS" — you should now see
   both `startupims_identity` and `startupims_core` listed, each with their tables
   underneath. If you can expand them and see the table names, this part is done correctly.

---

## Part 3 — Open the project in Visual Studio

1. Unzip `StartupIMS-backend.zip` somewhere permanent (not your Downloads folder —
   e.g. `C:\Projects\StartupIMS`).
2. Open **Visual Studio** → **Open a project or solution** → select `StartupIMS.sln`
   inside that folder.
3. Wait for it to finish loading. In the bottom-right status bar you may see
   "Restoring NuGet packages..." — let that finish before doing anything else.
4. In **Solution Explorer** (usually on the right), confirm you can see all four
   projects: `StartupIMS.API`, `StartupIMS.Domain`, `StartupIMS.Infrastructure`,
   `StartupIMS.Shared`.

---

## Part 4 — Install the two extra NuGet packages needed for scaffolding

We need two packages on the `StartupIMS.Infrastructure` project. Using the UI
(no typed commands needed):

1. In Solution Explorer, right-click **StartupIMS.Infrastructure** → **Manage NuGet Packages...**
2. Click the **Browse** tab at the top.
3. In the search box, type `Microsoft.EntityFrameworkCore.Design` → click it in the
   list → on the right, click **Install** → if a dialog pops up asking to accept
   license terms, click **I Accept**.
4. Search again for `Microsoft.EntityFrameworkCore.Tools` → **Install** → accept
   any license prompt.
5. Wait for the yellow "installing..." bar at the bottom to disappear.

---

## Part 5 — Scaffold the entities and DbContexts from your database

"Scaffolding" means: EF Core looks at your actual MySQL tables and auto-generates
matching C# classes for you.

1. Open the **Package Manager Console**. In Visual Studio's top menu:
   **Tools → NuGet Package Manager → Package Manager Console**. A panel opens at
   the bottom with a `PM>` prompt — this is just a command box, similar to typing
   in a terminal but VS-specific.
2. At the top of that panel there's a dropdown labeled **Default project** —
   change it to `StartupIMS.Infrastructure`. This matters: whatever project is
   selected there is where the generated files will land.
3. Copy-paste this command, **replacing `your_real_password` with your actual MySQL
   root password**, then press Enter:

   ```powershell
   Scaffold-DbContext "Server=localhost;Port=3306;Database=startupims_identity;User=root;Password=your_real_password;" Pomelo.EntityFrameworkCore.MySql -OutputDir Persistence/Scaffolded/Identity -Context IdentityDbContext -ContextDir Persistence -DataAnnotations -Force
   ```

   Wait for it to finish — you'll see text scroll by, then the `PM>` prompt returns.
   If you see red error text instead, stop and check: is your password correct?
   Is MySQL actually running? Did Part 2 succeed?

4. Run the second one the same way:

   ```powershell
   Scaffold-DbContext "Server=localhost;Port=3306;Database=startupims_core;User=root;Password=your_real_password;" Pomelo.EntityFrameworkCore.MySql -OutputDir Persistence/Scaffolded/Core -Context CoreDbContext -ContextDir Persistence -DataAnnotations -Force
   ```

5. Back in Solution Explorer, click the "Show All Files" icon at the top of the
   panel (or right-click the Infrastructure project → **Include In Project** on any
   greyed-out files) if the new files don't appear automatically. You should now
   see a new folder structure inside `StartupIMS.Infrastructure/Persistence/Scaffolded/`
   with `Identity` and `Core` subfolders, each containing `.cs` files — one per
   table, plus `IdentityDbContext.cs` / `CoreDbContext.cs`.

---

## Part 6 — Remove the old hand-written files (they're now duplicates)

The files I originally hand-wrote are now redundant with what scaffolding just
generated. Delete these (right-click → Delete in Solution Explorer):

- `StartupIMS.Domain/Entities/` — delete all `.cs` files inside (User.cs, Startup.cs, etc.)
- `StartupIMS.Infrastructure/Persistence/IdentityDbContext.cs` (the old one, NOT
  the new one inside `Persistence/Scaffolded/Identity/`)
- `StartupIMS.Infrastructure/Persistence/CoreDbContext.cs` (same — keep the scaffolded one)

Leave `StartupIMS.Domain/Enums/` alone for now — we'll deal with those next.

---

## Part 7 — Fix the connection string that scaffolding hardcoded

Scaffolding puts your MySQL password directly into the generated `DbContext` file,
in a method called `OnConfiguring`. This is insecure and needs to go.

1. Open `StartupIMS.Infrastructure/Persistence/Scaffolded/Identity/IdentityDbContext.cs`.
2. Find a method that looks like this near the top:
   ```csharp
   protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
   {
       if (!optionsBuilder.IsConfigured)
       {
           optionsBuilder.UseMySql("Server=localhost;Port=3306;Database=startupims_identity;User=root;Password=your_real_password;", ...);
       }
   }
   ```
3. Delete that entire method. We'll supply the connection string from `Program.cs`
   instead (already set up that way in the file I gave you) — this keeps your
   password out of source code entirely.
4. Repeat for `CoreDbContext.cs` in the `Scaffolded/Core` folder.

---

## Part 8 — Reconnect the constructor (small fix scaffolding sometimes needs)

Open each scaffolded `DbContext` and confirm it has a constructor like this
(scaffolding usually includes it automatically, but check):

```csharp
public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
{
}
```

If instead you see a parameterless constructor `public IdentityDbContext() { }`,
add the one above alongside it — `Program.cs` needs to be able to pass in the
connection string via this constructor.

---

## Part 9 — Set your real secrets (if you haven't already)

Right-click **StartupIMS.API** → **Manage User Secrets**, and make sure it contains:

```json
{
  "Jwt": {
    "Secret": "a-long-random-string-at-least-32-characters"
  },
  "ConnectionStrings": {
    "IdentityDb": "Server=localhost;Port=3306;Database=startupims_identity;User=root;Password=your_real_password;",
    "CoreDb": "Server=localhost;Port=3306;Database=startupims_core;User=root;Password=your_real_password;"
  }
}
```

---

## Part 10 — Build the project

1. Menu bar: **Build → Build Solution** (or press `Ctrl+Shift+B`).
2. Look at the **Error List** panel at the bottom. You're aiming for
   "0 Errors" — warnings are fine for now.
3. Common errors at this stage and what they mean:
   - `The type or namespace 'User' could not be found` — something in
     `AuthController.cs` or a service still references the old hand-written
     entity classes we deleted in Part 6. Update the `using` statement at the
     top of that file to point at the scaffolded namespace instead
     (e.g. `using StartupIMS.Infrastructure.Persistence.Scaffolded.Identity;`).
   - `Cannot convert UserRole to string` (or similar) — this is the enum issue:
     scaffolded tables store `Role` as plain `string`, not the `UserRole` enum.
     In `AuthController.cs`, change any `UserRole` references to just compare
     strings directly (e.g. `"Admin"`, `"Mentor"`, `"Founder"`) until we clean
     this up properly.

If you hit an error you can't figure out, paste the exact error text here and
I'll tell you exactly what to change.

---

## Part 11 — Run it

1. Confirm the startup project is set: right-click **StartupIMS.API** →
   **Set as Startup Project** (if it isn't already bold in Solution Explorer).
2. Press **F5** (or the green ▶ Play button at the top).
3. A browser window should open automatically to a Swagger page
   (something like `https://localhost:7xxx/swagger`). If it opens to a blank
   page or 404 instead, add `/swagger` to the end of the URL manually.
4. You should see a list of endpoints: `POST /api/Auth/register`,
   `POST /api/Auth/login`, `GET /api/Startups`, etc. This confirms the API
   is running and reading its structure from your database-first models.

---

## Part 12 — Test it actually works

1. On the Swagger page, click **POST /api/Auth/register** → **Try it out**.
2. Fill in the example JSON with a test user, e.g.:
   ```json
   {
     "name": "Test Admin",
     "email": "admin@test.com",
     "password": "Password123!",
     "role": "Admin"
   }
   ```
3. Click **Execute**. You should get a `200 OK` response with an `accessToken`
   in the response body.
4. Go back to MySQL Workbench, refresh `startupims_identity` → `Users` table
   (right-click the table → **Select Rows - Limit 1000**), and confirm your
   new test user is actually sitting in the database.

If that last step shows your row, the whole pipeline — Visual Studio → EF Core →
MySQL — is wired up correctly end to end.

---

## If something breaks

Come back and tell me exactly which **Part number** you're on and paste the exact
error message (screenshot is fine too) — don't just say "it doesn't work." The
error text almost always tells us precisely what's wrong.
