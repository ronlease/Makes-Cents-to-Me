# Makes Cents To Me -- Product Backlog

---

## Epic 1: Foundation -- Data Model and Institution Configuration

These items establish the core database schema and the ability to manage financial
institutions and accounts. Everything else depends on this foundation.

---

### [MCM-001] Create Core Database Schema

**Status:** Done
**Priority:** High

#### Business Problem
Before any data can be imported or analyzed, the system needs a database schema that
represents transactions, accounts, and institutions. Without this foundation, no other
feature can function. The schema uses a hybrid storage strategy: typed core columns for
commonly queried fields plus a `RawData` jsonb column that preserves the full raw CSV row
and any institution-specific extras. PostgreSQL jsonb operators allow querying the extras.
No dynamic DDL is required. Import profiles define which JSON paths map to which core fields.

#### Acceptance Criteria
```gherkin
Feature: Core database schema

  Scenario: Database contains institution table
    Given the application database has been initialized
    When I inspect the schema
    Then an "Institutions" table exists with columns for Id, Name, DateFormat, BalanceProvided, and AmountType

  Scenario: Database contains account table
    Given the application database has been initialized
    When I inspect the schema
    Then an "Accounts" table exists with columns for Id, InstitutionId, Name, and AccountType
    And AccountType supports at least Checking, Savings, MoneyMarket, and CreditCard

  Scenario: Database contains transaction table with hybrid storage
    Given the application database has been initialized
    When I inspect the schema
    Then a "Transactions" table exists with typed core columns for Id, AccountId, Date, Description, Amount, Balance, Category, RawDescription, CheckNumber, Principal, Interest, and Fees
    And the "Transactions" table has a "RawData" column of type jsonb
    And the RawData column stores the full raw CSV row verbatim as a JSON object

  Scenario: RawData preserves all institution-specific fields
    Given a CSV row contains columns "Date, Description, Amount, Balance, Memo, Reference Number"
    When the transaction is persisted
    Then the RawData jsonb column contains all original columns and values from the CSV row
    And the core typed columns (Date, Description, Amount, Balance) are also populated
    And institution-specific fields like "Memo" and "Reference Number" are queryable via jsonb operators

  Scenario: Credit card amount is computed from components
    Given a credit card transaction with Principal 45.00, Interest 1.50, and Fees 3.00
    When the transaction is persisted
    Then the Amount equals 49.50

  Scenario: Default values for interest and fees
    Given a credit card transaction where only Principal is provided
    When the transaction is persisted
    Then Interest defaults to 0
    And Fees defaults to 0
```

---

### [MCM-002] Manage Financial Institutions

**Status:** Done
**Priority:** High

#### Business Problem
The user works with multiple financial institutions, each with its own CSV format. The user
needs to create, view, update, and delete institutions so the system knows how to handle
imports from each one.

#### Acceptance Criteria
```gherkin
Feature: Financial institution management

  Scenario: Create a new institution
    Given no institution named "My Credit Union" exists
    When I create an institution with name "My Credit Union"
    Then the institution is saved successfully
    And it appears in the list of institutions

  Scenario: View all institutions
    Given institutions "My Credit Union" and "Chase" exist
    When I request the list of institutions
    Then both institutions are returned

  Scenario: Update an institution name
    Given an institution named "My Credit Union" exists
    When I update its name to "Primary Credit Union"
    Then the institution name is "Primary Credit Union"

  Scenario: Delete an institution with no linked accounts
    Given an institution named "Unused Bank" exists with no accounts
    When I delete the institution
    Then it is removed from the system

  Scenario: Prevent deletion of institution with linked accounts
    Given an institution named "My Credit Union" has linked accounts
    When I attempt to delete the institution
    Then the system rejects the deletion with a message explaining accounts must be removed first
```

---

### [MCM-003] Manage Accounts Under Institutions

**Status:** Done
**Priority:** High

#### Business Problem
Each institution can have multiple accounts (checking, savings, credit card, etc.). The user
needs to create and manage accounts so that imported transactions are associated with the
correct account.

#### Acceptance Criteria
```gherkin
Feature: Account management

  Scenario: Create a checking account under an institution
    Given an institution named "My Credit Union" exists
    When I create an account named "Primary Checking" of type Checking under that institution
    Then the account is saved and linked to "My Credit Union"

  Scenario: Create a credit card account under an institution
    Given an institution named "Chase" exists
    When I create an account named "Amazon Visa" of type CreditCard under that institution
    Then the account is saved and linked to "Chase"

  Scenario: View all accounts for an institution
    Given "My Credit Union" has accounts "Primary Checking" and "Savings"
    When I request accounts for "My Credit Union"
    Then both accounts are returned

  Scenario: Prevent duplicate account names within an institution
    Given "My Credit Union" already has an account named "Primary Checking"
    When I attempt to create another account named "Primary Checking" under "My Credit Union"
    Then the system rejects the creation with a duplicate name error

  Scenario: Delete an account with no transactions
    Given an account "Empty Savings" exists with no transactions
    When I delete the account
    Then it is removed from the system

  Scenario: Prevent deletion of account with transactions
    Given an account "Primary Checking" has imported transactions
    When I attempt to delete the account
    Then the system rejects the deletion with a message explaining transactions must be removed first
```

---

### [MCM-020] Light and Dark Mode Theme Support

**Status:** Done
**Priority:** High

#### Business Problem
The user wants light and dark mode from day one. The app should detect the system theme
preference on first load and allow the user to toggle between light and dark mode. This is
a foundational UX requirement -- not something to bolt on later.

#### Acceptance Criteria
```gherkin
Feature: Light and dark mode theme support

  Scenario: App detects system preference on first load
    Given the user has not set a theme preference in the application
    And the user's operating system is set to dark mode
    When the user loads the application for the first time
    Then the application renders in dark mode

  Scenario: App detects light system preference on first load
    Given the user has not set a theme preference in the application
    And the user's operating system is set to light mode
    When the user loads the application for the first time
    Then the application renders in light mode

  Scenario: User toggles between light and dark mode
    Given the application is currently in light mode
    When the user toggles the theme to dark mode
    Then the application switches to dark mode immediately

  Scenario: Theme preference persists across sessions
    Given the user has toggled the theme to dark mode
    When the user closes and reopens the application
    Then the application renders in dark mode
    And the theme preference is stored in localStorage

  Scenario: No bright flash on initial load for dark mode users
    Given the user's stored preference is dark mode
    When the application loads
    Then the user does not see a white screen flash before the dark theme is applied

  Scenario: Angular Material theming supports both modes
    Given Angular Material is used for UI components
    When the application is in dark mode
    Then all Angular Material components render with the dark theme
    When the application is in light mode
    Then all Angular Material components render with the light theme
```

---

### [MCM-021] Redesign Frontend UI

**Status:** Backlog
**Priority:** Medium

#### Business Problem
The current UI uses default Angular Material styling with minimal design polish. The
application needs a visual redesign to be more attractive and usable -- better layout,
typography, spacing, color palette, and overall polish. This is a cosmetic overhaul, not a
functional change. A well-designed interface builds confidence in the tool and makes daily
use more pleasant.

#### Acceptance Criteria
```gherkin
Feature: Frontend UI redesign

  Scenario: Consistent visual theme across all pages
    Given the application has been redesigned
    When I navigate between any pages in the application
    Then every page uses a consistent color palette, typography, and spacing
    And no page falls back to unstyled or default Angular Material appearance

  Scenario: Responsive layout adapts to different screen sizes
    Given the application has been redesigned
    When I view the application on a screen width of 1920 pixels
    Then the layout uses available space effectively without excessive whitespace
    When I view the application on a screen width of 768 pixels
    Then the layout reflows gracefully without horizontal scrolling or overlapping elements

  Scenario: Navigation is clear and accessible
    Given the application has been redesigned
    When I open the application
    Then the primary navigation is visible and clearly indicates the current active page
    And all navigation links have readable labels and adequate click targets

  Scenario: Component styling is polished and intentional
    Given the application has been redesigned
    When I interact with buttons, tables, forms, and cards throughout the application
    Then all components have consistent border radius, shadow, padding, and hover states
    And interactive elements provide clear visual feedback on hover, focus, and active states
```

---

## Epic 2: CSV Import Pipeline

These items build the import pipeline from file upload through column mapping, parsing,
and deduplication. This is the primary way data enters the system.

---

### [MCM-004] Upload CSV and Select Account

**Status:** Done
**Priority:** High

#### Business Problem
The user needs to upload a CSV file from a financial institution and have the system read
its column headers. This is the first step of every import and the foundation for column
mapping. The user must explicitly select an institution and account before uploading so
that the file is associated with the correct account and the correct import profile is
applied.

#### Acceptance Criteria
```gherkin
Feature: CSV upload with account selection and header detection

  Scenario: User selects institution then account before upload
    Given institutions "My Credit Union" and "Chase" exist
    And "My Credit Union" has accounts "Primary Checking" and "Savings"
    When I begin a new import
    Then I must first select an institution
    And then select an account under that institution
    And only then can I upload a CSV file

  Scenario: Upload a valid CSV file for a selected account
    Given I have selected institution "My Credit Union" and account "Primary Checking"
    And I have a CSV file from that account
    When I upload the file
    Then the system reads the file and returns the list of column headers
    And the raw file content is preserved for subsequent processing
    And the file is associated with account "Primary Checking"

  Scenario: Reject an empty file
    Given I have selected an institution and account
    And I have an empty file
    When I upload the file
    Then the system rejects it with a message "File is empty"

  Scenario: Reject a non-CSV file
    Given I have selected an institution and account
    And I have a PDF file
    When I upload the file
    Then the system rejects it with a message indicating only CSV files are accepted

  Scenario: Handle trailing empty rows
    Given I have selected an institution and account
    And I have a CSV file with trailing empty rows
    When I upload the file
    Then the trailing empty rows are ignored
    And only rows with data are processed
```

---

### [MCM-005] User-Defined Schema Mapping for Import Profiles

**Status:** Done
**Priority:** High

#### Business Problem
Different institutions export CSVs with different column names, layouts, and
institution-specific fields. The user needs to define how each CSV column maps to the
application's data structure -- not just map to predefined field names, but define the
schema itself. The system draws inferences from the uploaded file (detects headers,
suggests data types) to assist the user. This schema definition is saved as an import
profile on the account so the user only has to do it once per account. Fields beyond the
core set (Date, Description, Amount, Balance, Category, CheckNumber, Principal, Interest,
Fees) are stored in the jsonb RawData column and remain queryable.

#### Acceptance Criteria
```gherkin
Feature: User-defined schema mapping and import profiles

  Scenario: First import requires user-defined schema mapping
    Given account "Primary Checking" has no saved import profile
    When I upload a CSV with headers "Date, Transaction Description, Amount, Balance, Check/Misc., Note, Category"
    Then the system detects the headers and suggests data types for each column
    And I can map "Date" to Date, "Transaction Description" to Description, "Amount" to Amount, "Balance" to Balance, "Check/Misc." to CheckNumber, and "Category" to Category
    And I can leave "Note" unmapped, and it will be stored in the RawData jsonb column

  Scenario: System infers data types from file content
    Given account "Primary Checking" has no saved import profile
    When I upload a CSV file
    Then the system examines sample values and suggests data types (date, numeric, text) for each column
    And the user can accept or override each suggestion

  Scenario: Save import profile after first mapping
    Given I have completed a schema mapping for "Primary Checking"
    When I confirm the mapping
    Then the mapping is saved as the import profile for that account
    And the profile records the date format, whether balance is provided, and whether amount is single or split
    And the profile defines which JSON paths in RawData map to which core fields

  Scenario: Subsequent imports use saved profile automatically
    Given account "Primary Checking" has a saved import profile
    When I upload a new CSV file for that account
    Then the system applies the saved mapping without prompting

  Scenario: Reject CSV only when mapped columns are missing
    Given account "Primary Checking" has a saved profile expecting mapped columns "Date, Transaction Description, Amount, Balance"
    When I upload a CSV that is missing the "Balance" column
    Then the system rejects the file with a message indicating the mapped column "Balance" is missing

  Scenario: Accept CSV with unexpected new columns
    Given account "Primary Checking" has a saved profile expecting mapped columns "Date, Transaction Description, Amount, Balance"
    When I upload a CSV with headers "Date, Transaction Description, Amount, Balance, Rewards Points"
    Then the system accepts the file
    And the unexpected column "Rewards Points" is stored in the RawData jsonb column

  Scenario: Update an existing import profile
    Given account "Primary Checking" has a saved import profile
    When I choose to update the mapping
    Then I can redefine the schema mapping and save the updated profile

  Scenario: Map split amount columns for credit card
    Given account "Credit Card" has no saved import profile
    When I upload a CSV with headers "Date, Transaction Description, Principal, Interest, Fees, Balance, Check/Misc., Note, Category"
    Then I can map "Principal" to Principal, "Interest" to Interest, and "Fees" to Fees
    And the profile records the amount type as split
```

---

### [MCM-006] Parse CSV Using Import Profile

**Status:** Done
**Priority:** High

#### Business Problem
Once column mapping is established, the system needs to parse the CSV rows into the
internal transaction schema. Each row must be converted correctly based on the import
profile, preserving raw data while normalizing into the common structure. When the
institution does not provide a per-row balance, the user supplies either an opening
balance or a closing balance, and the system computes running balances forward or backward
accordingly.

#### Acceptance Criteria
```gherkin
Feature: CSV parsing with import profile

  Scenario: Parse checking account CSV rows
    Given account "Primary Checking" has a saved import profile
    And a CSV file contains rows with dates in "M/D/YYYY 0:00" format
    When the file is parsed
    Then each row is converted to a transaction with Date, Description, Amount, Balance, and CheckNumber
    And the raw description is preserved verbatim in RawDescription
    And the full raw CSV row is stored in the RawData jsonb column

  Scenario: Parse credit card CSV with split amount
    Given account "CU Credit Card" has a profile with split amount type
    And a CSV row has Principal 45.00, Interest 1.50, Fees 3.00
    When the row is parsed
    Then the transaction Amount is computed as 49.50
    And Principal, Interest, and Fees are stored individually

  Scenario: Parse Chase Visa CSV with single amount
    Given account "Amazon Visa" has a profile with single amount type
    And a CSV row has Amount -52.34
    When the row is parsed
    Then Principal is set to -52.34
    And Interest and Fees default to 0

  Scenario: Compute balance forward from opening balance
    Given account "Amazon Visa" has a profile where balance is not provided
    And the user supplies an opening balance of 1000.00
    And the CSV contains 3 transactions with amounts -50.00, -25.00, and -100.00 in chronological order
    When the file is parsed
    Then the system computes running balances forward: 950.00, 925.00, and 825.00

  Scenario: Compute balance backward from closing balance
    Given account "Amazon Visa" has a profile where balance is not provided
    And the user supplies a closing balance of 825.00
    And the CSV contains 3 transactions with amounts -50.00, -25.00, and -100.00 in chronological order
    When the file is parsed
    Then the system computes running balances backward: 950.00, 925.00, and 825.00

  Scenario: Parse dates correctly across formats
    Given account "Primary Checking" uses date format "M/D/YYYY 0:00"
    And account "Amazon Visa" uses date format "M/D/YYYY"
    When CSV files for each account are parsed
    Then all dates are stored as proper date values regardless of source format

  Scenario: Reject rows with invalid data
    Given a CSV row has a non-numeric value in the Amount column
    When the row is parsed
    Then the system flags the row as an error
    And continues parsing remaining rows
    And reports all errors at the end
```

---

### [MCM-007] Deduplicate Transactions on Import

**Status:** Done
**Priority:** High

#### Business Problem
Financial institution CSV exports often contain overlapping date ranges with previous
exports. If the user re-imports data, the system must not create duplicate transactions.
A reliable deduplication mechanism is essential for data integrity.

#### Acceptance Criteria
```gherkin
Feature: Transaction deduplication

  Scenario: Detect duplicate transaction on import
    Given a transaction with Date 3/15/2026, Description "GROCERY STORE", and Amount -85.42 already exists for "Primary Checking"
    When I import a CSV containing an identical row
    Then the duplicate row is skipped
    And the system reports it was identified as a duplicate

  Scenario: Allow non-duplicate with same description but different date
    Given a transaction with Date 3/15/2026 and Description "GROCERY STORE" exists
    When I import a row with Date 3/16/2026 and Description "GROCERY STORE" and the same Amount
    Then the row is treated as a new transaction

  Scenario: Handle multiple identical transactions on the same day
    Given two transactions with identical Date, Description, and Amount exist for the same account
    When I import a CSV that also contains two such rows
    Then the system matches them correctly and does not create extras

  Scenario: Report dedup summary after import
    Given a CSV file contains 50 rows, of which 10 are duplicates
    When the file is imported
    Then the system reports 40 new transactions and 10 duplicates skipped
```

---

## Epic 3: Canonical Categories

Before Claude can categorize transactions, the user needs a set of canonical categories
to map into.

---

### [MCM-008] Manage Canonical Categories

**Status:** Done
**Priority:** High

#### Business Problem
The user needs a consistent set of spending categories that all transactions are mapped
into, regardless of the institution-provided category. This canonical set enables
meaningful cross-institution reporting and trend analysis.

#### Acceptance Criteria
```gherkin
Feature: Canonical category management

  Scenario: Seed default categories
    Given the application is initialized for the first time
    When the database is seeded
    Then the following default categories exist: Groceries, Dining, Transportation, Utilities, Housing, Insurance, Healthcare, Entertainment, Shopping, Travel, Personal Care, Education, Gifts and Donations, Fees and Charges, Income, Transfer, Uncategorized

  Scenario: Add a custom category
    Given the default categories exist
    When I add a category named "Pet Care"
    Then "Pet Care" appears in the list of categories

  Scenario: Rename a category
    Given a category "Dining" exists
    When I rename it to "Dining and Restaurants"
    Then the category name is updated
    And all transactions previously mapped to "Dining" now reflect the new name

  Scenario: Prevent duplicate category names
    Given a category "Groceries" exists
    When I attempt to add another category named "Groceries"
    Then the system rejects it with a duplicate name error

  Scenario: Prevent deletion of category with assigned transactions
    Given category "Groceries" has transactions assigned to it
    When I attempt to delete it
    Then the system rejects the deletion with a message explaining transactions must be reassigned first

  Scenario: Delete an unused category
    Given category "Pet Care" has no assigned transactions
    When I delete it
    Then it is removed from the system
```

---

## Epic 4: Claude Integration -- Vendor Normalization and Categorization

These items add the intelligence layer. Claude analyzes transactions to suggest clean
vendor names and canonical categories.

---

### [MCM-009] Claude Analyzes Transactions for Vendor and Category

**Status:** Done
**Priority:** High

#### Business Problem
Raw transaction descriptions from financial institutions are messy and inconsistent
(e.g., "AMZN MKTP US*2K4F7B1G3" should become "Amazon"). The user needs the system to
automatically suggest a clean vendor name and a canonical category for each imported
transaction so they do not have to manually classify hundreds of transactions.

#### Acceptance Criteria
```gherkin
Feature: Claude transaction analysis

  Scenario: Claude suggests vendor and category for a new transaction
    Given a new transaction with raw description "AMZN MKTP US*2K4F7B1G3"
    When Claude analyzes the transaction
    Then Claude returns a suggested normalized vendor of "Amazon"
    And a suggested canonical category of "Shopping"
    And a confidence level for each suggestion

  Scenario: Claude processes a batch of transactions
    Given 25 new transactions have been parsed from a CSV import
    When Claude analyzes the batch
    Then each transaction receives a suggested vendor and category
    And the system handles rate limits and retries gracefully

  Scenario: Claude analysis respects existing learned rules
    Given a learned rule maps "AMZN MKTP" to vendor "Amazon" and category "Shopping"
    And a new transaction has raw description "AMZN MKTP US*9X8Y7Z6W5"
    When the system processes the transaction
    Then the learned rule is applied without calling Claude
    And the transaction is marked as auto-categorized

  Scenario: Handle Claude API unavailability
    Given the Claude API is unreachable
    When a batch of transactions is submitted for analysis
    Then the transactions are queued with status "Pending Analysis"
    And the system notifies the user that analysis is temporarily unavailable
    And the user can retry analysis later
```

---

## Epic 5: Review Queue

After Claude provides suggestions, the user reviews and accepts or overrides them.

---

### [MCM-010] Review Queue for Imported Transactions

**Status:** Done
**Priority:** High

#### Business Problem
The user needs to review Claude's suggestions before transactions are finalized. This
ensures data quality and gives the user control over how their transactions are
categorized. The review queue is the checkpoint between raw import and committed data.

#### Acceptance Criteria
```gherkin
Feature: Transaction review queue

  Scenario: Imported transactions appear in review queue
    Given 40 new transactions have been imported and analyzed by Claude
    When I open the review queue
    Then all 40 transactions are listed with their suggested vendor and category
    And each shows the raw description alongside the suggestion
    And each shows Claude's confidence level

  Scenario: Accept a suggestion
    Given a transaction in the review queue has suggested vendor "Amazon" and category "Shopping"
    When I accept the suggestion
    Then the transaction is committed with vendor "Amazon" and category "Shopping"
    And it is removed from the review queue

  Scenario: Override a suggestion
    Given a transaction has suggested vendor "Amazon" and category "Shopping"
    When I override the category to "Gifts and Donations"
    Then the transaction is committed with vendor "Amazon" and category "Gifts and Donations"
    And it is removed from the review queue

  Scenario: Bulk accept all suggestions
    Given 30 transactions in the review queue all have high confidence
    When I choose to accept all
    Then all 30 transactions are committed with their suggested values

  Scenario: Review queue persists across sessions
    Given 15 transactions are in the review queue
    When I close and reopen the application
    Then the same 15 transactions are still in the review queue with their suggestions
```

---

### [MCM-011] Promote Corrections to Learned Rules

**Status:** Backlog
**Priority:** High

#### Business Problem
When the user overrides Claude's suggestion, that correction represents valuable
knowledge. The system should learn from corrections so that future transactions with
similar descriptions are categorized correctly without needing Claude or manual review.

#### Acceptance Criteria
```gherkin
Feature: Learned rules from corrections

  Scenario: Override creates a learned rule suggestion
    Given I override a transaction with raw description "WHOLEFDS MKT 10245" from category "Shopping" to "Groceries" with vendor "Whole Foods"
    When the override is committed
    Then the system suggests creating a learned rule for pattern "WHOLEFDS MKT" mapping to vendor "Whole Foods" and category "Groceries"

  Scenario: Accept a learned rule
    Given the system has suggested a learned rule for "WHOLEFDS MKT"
    When I accept the rule
    Then it is saved as a learned rule
    And future transactions matching "WHOLEFDS MKT" are auto-categorized without Claude

  Scenario: View all learned rules
    Given 10 learned rules have been created
    When I view the learned rules list
    Then all 10 rules are displayed with their pattern, vendor, and category

  Scenario: Edit a learned rule
    Given a learned rule maps "WHOLEFDS MKT" to category "Groceries"
    When I update the category to "Dining and Restaurants"
    Then the rule is updated
    And future matching transactions use the new category

  Scenario: Delete a learned rule
    Given a learned rule for "WHOLEFDS MKT" exists
    When I delete the rule
    Then it is removed
    And future matching transactions are sent to Claude for analysis again
```

---

## Epic 6: Transaction Management

Once transactions are committed, the user needs to view and manage them.

---

### [MCM-012] View and Search Transactions

**Status:** Backlog
**Priority:** Medium

#### Business Problem
The user needs to browse their committed transactions with filtering and search
capabilities. This is the primary way to verify imported data and find specific
transactions.

#### Acceptance Criteria
```gherkin
Feature: Transaction viewing and search

  Scenario: View transactions for an account
    Given "Primary Checking" has 200 committed transactions
    When I view transactions for "Primary Checking"
    Then transactions are displayed in reverse chronological order
    And results are paginated

  Scenario: Filter transactions by date range
    Given transactions span from January 2025 to March 2026
    When I filter by date range February 1 2026 to February 28 2026
    Then only transactions within that range are returned

  Scenario: Filter transactions by category
    Given transactions exist across multiple categories
    When I filter by category "Groceries"
    Then only transactions categorized as "Groceries" are returned

  Scenario: Search transactions by vendor name
    Given transactions exist with vendors "Amazon", "Whole Foods", and "Target"
    When I search for "Amazon"
    Then only transactions with vendor "Amazon" are returned

  Scenario: View transactions across all accounts
    Given transactions exist in "Primary Checking" and "Amazon Visa"
    When I view all transactions without specifying an account
    Then transactions from all accounts are returned
```

---

## Epic 7: Spending Intelligence and Reporting

These items deliver the analytical value of the application -- the reason it exists.

---

### [MCM-013] Spending by Category Report

**Status:** Backlog
**Priority:** Medium

#### Business Problem
**Note:** This is a placeholder backlog item. Detailed requirements will be defined later.

The user needs to see how much they are spending in each category over a given time
period. This is the most fundamental spending insight and the foundation for identifying
where money goes.

#### Acceptance Criteria
```gherkin
Feature: Spending by category report

  Scenario: View monthly spending breakdown by category
    Given committed transactions exist for March 2026
    When I view the spending by category report for March 2026
    Then each canonical category is listed with its total spending amount
    And categories are sorted by amount descending
    And a total across all categories is displayed

  Scenario: View spending by category for a custom date range
    Given committed transactions exist across multiple months
    When I view the spending by category report for January 1 2026 to March 31 2026
    Then spending is aggregated across the full date range per category

  Scenario: Exclude income and transfer categories from spending total
    Given transactions include Income and Transfer categories
    When I view the spending report
    Then Income and Transfer amounts are shown separately
    And they are excluded from the spending total
```

---

### [MCM-014] Spending Trend Over Time

**Status:** Backlog
**Priority:** Medium

#### Business Problem
**Note:** This is a placeholder backlog item. Detailed requirements will be defined later.

The user needs to see how their spending changes month over month to identify upward or
downward trends. This helps them understand whether their spending is increasing and in
which categories.

#### Acceptance Criteria
```gherkin
Feature: Spending trend over time

  Scenario: View monthly spending trend
    Given committed transactions exist for the last 12 months
    When I view the spending trend report
    Then a chart displays total monthly spending for each of the last 12 months

  Scenario: View trend for a specific category
    Given committed transactions exist for "Groceries" over the last 12 months
    When I view the trend for "Groceries"
    Then a chart displays monthly spending in "Groceries" for each of the last 12 months

  Scenario: Trend line shows direction
    Given monthly spending data exists for the last 6 months
    When I view the trend report
    Then a trend line is overlaid on the chart indicating the overall spending direction
```

---

### [MCM-015] Personal Inflation Rate

**Status:** Backlog
**Priority:** Low

#### Business Problem
**Note:** This is a placeholder backlog item. Detailed requirements will be defined later.

The user wants to understand how their personal cost of living is changing over time,
independent of official inflation numbers. By comparing the same category spending across
periods, the system can surface a personal inflation rate that reflects the user's actual
experience.

#### Acceptance Criteria
```gherkin
Feature: Personal inflation rate

  Scenario: Calculate year-over-year personal inflation
    Given committed transactions exist for March 2025 and March 2026
    When I view my personal inflation rate for March 2026 compared to March 2025
    Then the system shows the percentage change in total spending
    And breaks it down by category

  Scenario: Calculate rolling 12-month inflation
    Given committed transactions exist for the last 24 months
    When I view my rolling 12-month personal inflation rate
    Then the system compares the most recent 12 months to the prior 12 months
    And shows an overall percentage change and per-category breakdown

  Scenario: Exclude one-time large purchases from inflation calculation
    Given a single transaction of 5000 for "Emergency Car Repair" exists in March 2026
    When I view my personal inflation rate
    Then the system flags the outlier
    And shows the inflation rate both with and without the outlier
```

---

### [MCM-016] Anomaly Detection and Alerts

**Status:** Backlog
**Priority:** Low

#### Business Problem
**Note:** This is a placeholder backlog item. Detailed requirements will be defined later.

The user wants to be notified when spending in a category is unusually high compared to
their historical average. Early detection of anomalies helps the user course-correct
before overspending becomes a pattern.

#### Acceptance Criteria
```gherkin
Feature: Spending anomaly alerts

  Scenario: Alert on unusually high category spending
    Given the user's average monthly spending on "Dining" is 200 over the last 6 months
    And March 2026 spending on "Dining" is 450
    When the anomaly detection runs
    Then the system generates an alert for "Dining" spending being significantly above average

  Scenario: No alert for normal spending variation
    Given the user's average monthly spending on "Groceries" is 500
    And March 2026 spending on "Groceries" is 530
    When the anomaly detection runs
    Then no alert is generated for "Groceries"

  Scenario: View active alerts
    Given two anomaly alerts have been generated
    When I view the alerts dashboard
    Then both alerts are displayed with category, amount, average, and percentage above average

  Scenario: Dismiss an alert
    Given an alert for "Dining" exists
    When I dismiss the alert
    Then it is removed from the active alerts list
```

---

### [MCM-017] Recurring Transaction Detection

**Status:** Backlog
**Priority:** Low

#### Business Problem
**Note:** This is a placeholder backlog item. Detailed requirements will be defined later.

The user wants the system to identify recurring bills and subscriptions automatically.
Knowing which transactions repeat on a regular schedule helps the user track fixed
expenses, spot forgotten subscriptions, and notice when a recurring charge changes in
amount.

#### Acceptance Criteria
```gherkin
Feature: Recurring transaction detection

  Scenario: Identify a monthly recurring transaction
    Given transactions with vendor "Netflix" and amount 15.99 appear on or near the 5th of each month for the last 4 months
    When the recurring detection runs
    Then "Netflix" is identified as a monthly recurring transaction of 15.99

  Scenario: Detect a recurring transaction with amount change
    Given "Netflix" has been recurring at 15.99 monthly
    And the latest charge is 17.99
    When the recurring detection runs
    Then the system flags that "Netflix" has increased from 15.99 to 17.99

  Scenario: View all recurring transactions
    Given 8 recurring transactions have been identified
    When I view the recurring transactions list
    Then all 8 are displayed with vendor, amount, frequency, and next expected date

  Scenario: Dismiss a false positive recurring transaction
    Given "Gas Station" has been flagged as recurring
    When I dismiss it as not recurring
    Then it is removed from the recurring list
    And the system does not flag it again unless the user resets
```

---

### [MCM-018] Spending Projection

**Status:** Backlog
**Priority:** Low

#### Business Problem
**Note:** This is a placeholder backlog item. Detailed requirements will be defined later.

The user wants to see a projection of their spending for the remainder of the current
month based on their pace so far. This helps them make informed decisions about whether to
cut back before the month ends.

#### Acceptance Criteria
```gherkin
Feature: Spending projection

  Scenario: Project end-of-month spending
    Given today is March 15 2026
    And spending so far in March is 1500
    When I view the spending projection for March 2026
    Then the system projects approximately 3000 for the full month based on the current daily pace

  Scenario: Project spending by category
    Given today is March 20 2026
    And spending on "Groceries" so far in March is 350
    When I view the category projection
    Then the system projects approximately 542 for "Groceries" for the full month

  Scenario: Compare projection to historical average
    Given the projection for March 2026 is 3000
    And the average monthly spending over the last 6 months is 2700
    When I view the projection
    Then the system notes that projected spending is 11 percent above the recent average
```

---

## Epic 8: Dashboard

The dashboard ties all reports and alerts together into a single view.

---

### [MCM-019] Main Dashboard

**Status:** Backlog
**Priority:** Low

#### Business Problem
**Note:** This is a placeholder backlog item. Detailed requirements will be defined later.

The user needs a single landing page that provides an at-a-glance summary of their
financial picture. Rather than navigating to individual reports, the dashboard surfaces
the most important information upfront: current month spending, category breakdown,
active alerts, and recent transactions.

#### Acceptance Criteria
```gherkin
Feature: Main dashboard

  Scenario: Dashboard displays current month summary
    Given committed transactions exist for March 2026
    When I open the dashboard
    Then the dashboard shows total spending for March 2026
    And a category breakdown chart for the current month
    And the spending projection for the remainder of the month

  Scenario: Dashboard displays active alerts
    Given two anomaly alerts exist
    When I open the dashboard
    Then both alerts are prominently displayed

  Scenario: Dashboard displays recent transactions
    Given committed transactions exist
    When I open the dashboard
    Then the 10 most recent transactions are listed

  Scenario: Dashboard displays month-over-month trend
    Given spending data exists for the last 6 months
    When I open the dashboard
    Then a trend chart shows monthly spending totals for the last 6 months
```

---

## Backlog Summary

| ID      | Title                                           | Priority | Status  |
|---------|-------------------------------------------------|----------|---------|
| MCM-001 | Create Core Database Schema                     | High     | Done    |
| MCM-002 | Manage Financial Institutions                   | High     | Done    |
| MCM-003 | Manage Accounts Under Institutions              | High     | Done    |
| MCM-004 | Upload CSV and Select Account                   | High     | Done    |
| MCM-005 | User-Defined Schema Mapping for Import Profiles | High     | Done    |
| MCM-006 | Parse CSV Using Import Profile                  | High     | Done    |
| MCM-007 | Deduplicate Transactions on Import              | High     | Done    |
| MCM-008 | Manage Canonical Categories                     | High     | Done    |
| MCM-009 | Claude Analyzes Transactions                    | High     | Done    |
| MCM-010 | Review Queue for Imported Transactions          | High     | Done    |
| MCM-011 | Promote Corrections to Learned Rules            | High     | Backlog |
| MCM-012 | View and Search Transactions                    | Medium   | Backlog |
| MCM-013 | Spending by Category Report (placeholder)       | Medium   | Backlog |
| MCM-014 | Spending Trend Over Time (placeholder)          | Medium   | Backlog |
| MCM-015 | Personal Inflation Rate (placeholder)           | Low      | Backlog |
| MCM-016 | Anomaly Detection and Alerts (placeholder)      | Low      | Backlog |
| MCM-017 | Recurring Transaction Detection (placeholder)   | Low      | Backlog |
| MCM-018 | Spending Projection (placeholder)               | Low      | Backlog |
| MCM-019 | Main Dashboard (placeholder)                    | Low      | Backlog |
| MCM-020 | Light and Dark Mode Theme Support               | High     | Done    |
| MCM-021 | Redesign Frontend UI                            | Medium   | Backlog |
