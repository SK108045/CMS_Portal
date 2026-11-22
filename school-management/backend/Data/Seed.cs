using Microsoft.AspNetCore.Identity;
using School.Api.Models;

namespace School.Api.Data;

public static class Seed
{
    public static async Task Run(SchoolDb db, IPasswordHasher<User> hash)
    {
        if (db.Users.Any())
            return;
        var names = new[]
        {
            "Administration",
            "Finance",
            "Teaching & Learning",
            "Kitchen",
            "Maintenance",
            "ICT",
            "Transport",
            "Student Welfare",
            "Chapel / Mission Activities",
            "Stores",
        };
        foreach (var n in names)
            db.Departments.Add(new Department { Name = n });
        foreach (
            var n in new[]
            {
                "Salaries / Staff Costs",
                "Learning Materials",
                "Textbooks",
                "Kitchen / Food Supplies",
                "Student Welfare",
                "Maintenance",
                "Utilities",
                "Water",
                "Electricity",
                "ICT",
                "Transport",
                "Fuel",
                "Security",
                "Cleaning Supplies",
                "Office / Administration",
                "Chapel / Mission Activities",
                "Furniture",
                "Construction / Development",
                "Medical / First Aid",
                "Sports / Activities",
                "Other",
            }
        )
            db.Categories.Add(new Category { Name = n });
        foreach (
            var n in new[]
            {
                "Nairobi School Supplies Ltd",
                "Eastlands Hardware & Electrical",
                "Dandora Food Distributors",
                "Metro ICT Solutions",
                "Mission Books Kenya",
                "Nairobi Fuel Services",
            }
        )
            db.Suppliers.Add(
                new Supplier
                {
                    Name = n,
                    Phone = "+254700123456",
                    Notes = "Demo school supplier",
                }
            );
        db.Locations.Add(new StockLocation { Name = "Main school stores" });
        await db.SaveChangesAsync();
        var admin = new User
        {
            Name = "School Administrator",
            Email = "admin@missionschool.local",
            Role = "ADMIN",
            Position = "School Director",
        };
        admin.PasswordHash = hash.HashPassword(admin, "Admin123!");
        db.Users.Add(admin);
        foreach (var n in new[] { "Kitchen", "Maintenance", "ICT", "Stores" })
        {
            var u = new User
            {
                Name = n + " Officer",
                Email = n.ToLower() + "@missionschool.local",
                DepartmentId = db.Departments.Single(d => d.Name == n).Id,
                Position = "Department Officer",
            };
            u.PasswordHash = hash.HashPassword(u, "SchoolDemo123!");
            db.Users.Add(u);
        }
        foreach (var profile in new[] { ("finance", "FINANCE", "Finance"), ("storekeeper", "STOREKEEPER", "Stores"), ("head", "DEPARTMENT_HEAD", "Kitchen"), ("auditor", "AUDITOR", "") })
        {
            var u = new User { Name = profile.Item2 + " Demo", Email = profile.Item1 + "@missionschool.local", Role = profile.Item2, DepartmentId = profile.Item3 == "" ? null : db.Departments.Single(d => d.Name == profile.Item3).Id, Position = profile.Item2 };
            u.PasswordHash = hash.HashPassword(u, "SchoolDemo123!");
            db.Users.Add(u);
        }
        await db.SaveChangesAsync();
        foreach (var d in db.Departments.ToList())
        {
            var cat = db.Categories.First(c =>
                c.Name
                == (
                    d.Name == "Kitchen" ? "Kitchen / Food Supplies"
                    : d.Name == "ICT" ? "ICT"
                    : d.Name == "Maintenance" ? "Maintenance"
                    : "Learning Materials"
                )
            );
            db.Budgets.Add(
                new Budget
                {
                    Name = d.Name + " annual allocation",
                    DepartmentId = d.Id,
                    CategoryId = cat.Id,
                    Amount =
                        d.Name == "Kitchen" ? 2400000
                        : d.Name == "Maintenance" ? 2000000
                        : 1000000,
                }
            );
            db.PettyAccounts.Add(
                new PettyAccount { Name = d.Name + " imprest", DepartmentId = d.Id }
            );
        }
        await db.SaveChangesAsync();
        var purposes = new[]
        {
            "20 Mathematics textbooks",
            "Maize flour and cooking oil",
            "Cleaning supplies",
            "Classroom desks",
            "Generator servicing",
            "Printer toner",
            "Science laboratory materials",
            "Fuel for school transport",
            "Water pump repair",
            "Chapel materials",
        };
        for (int i = 0; i < purposes.Length; i++)
        {
            var departmentNames = new[]
            {
                "Teaching & Learning",
                "Kitchen",
                "Kitchen",
                "Teaching & Learning",
                "Maintenance",
                "ICT",
                "Teaching & Learning",
                "Transport",
                "Maintenance",
                "Chapel / Mission Activities",
            };
            var departmentId = db.Departments.Single(d => d.Name == departmentNames[i]).Id;
            var b = db.Budgets.First(b => b.DepartmentId == departmentId);
            db.Requests.Add(
                new PurchaseRequest
                {
                    DepartmentId = b.DepartmentId!.Value,
                    BudgetId = b.Id,
                    RequestedBy = admin.Id,
                    Purpose = purposes[i],
                    EstimatedAmount = 20000 + i * 3500,
                    Status = i < 3 ? "Submitted" : "Draft",
                    SupplierId = db
                        .Suppliers.OrderBy(s => s.Id)
                        .ToList()[
                            i == 1 ? 2
                            : i == 4 || i == 8 ? 1
                            : i == 5 ? 3
                            : i == 7 ? 5
                            : 0
                        ]
                        .Id,
                    CreatedBy = admin.Id,
                    Items =
                    [
                        new RequestItem
                        {
                            Description = purposes[i],
                            Quantity = 1,
                            UnitPrice = 20000 + i * 3500,
                        },
                    ],
                }
            );
        }
        var loc = db.Locations.First();
        var stores = db.Departments.Single(x => x.Name == "Stores");
        var desks = new InventoryItem
        {
            Name = "Classroom desks",
            Code = "FUR-DESK",
            Category = "Permanent Items",
            LocationId = loc.Id,
            DepartmentId = stores.Id,
            ReorderLevel = 5,
        };
        var computers = new InventoryItem
        {
            Name = "Donated computers",
            Code = "ICT-PC",
            Category = "Permanent Items",
            LocationId = loc.Id,
            DepartmentId = stores.Id,
        };
        db.Inventory.AddRange(desks, computers);
        await db.SaveChangesAsync();
        db.Income.AddRange(
            new Income
            {
                Source = "Local church",
                Donor = "Mission Church Kenya",
                Type = "Donation",
                Amount = 250000,
                Purpose = "School operations",
                Reference = "CHURCH-2026-001",
            },
            new Income
            {
                Source = "Mission partner",
                Donor = "Friends of the Mission",
                Type = "Non-cash Donation",
                Amount = 0,
                Quantity = 25,
                InventoryItemId = desks.Id,
                Purpose = "Classroom furniture",
                DepartmentId = stores.Id,
            },
            new Income
            {
                Source = "Education sponsor",
                Donor = "ICT Mission Partner",
                Type = "Non-cash Donation",
                Quantity = 10,
                InventoryItemId = computers.Id,
                Purpose = "Computer laboratory",
                Restricted = true,
                DepartmentId = stores.Id,
            }
        );
        db.Movements.AddRange(
            new StockMovement
            {
                InventoryItemId = desks.Id,
                Quantity = 25,
                Kind = "Donated",
                DepartmentId = stores.Id,
                Source = "Mission partner",
            },
            new StockMovement
            {
                InventoryItemId = computers.Id,
                Quantity = 10,
                Kind = "Donated",
                DepartmentId = stores.Id,
                Source = "Education sponsor",
            }
        );
        db.Assets.Add(
            new Asset
            {
                Name = "Generator #2",
                Code = "GEN-002",
                DepartmentId = db.Departments.Single(x => x.Name == "Maintenance").Id,
                AcquisitionCost = 450000,
                Location = "Utility building",
            }
        );
        db.Audit.Add(
            new AuditLog
            {
                Actor = "Seed command",
                Action = "Demo seeded",
                Entity = "system",
                Description = "Mission school demonstration records",
            }
        );
        await db.SaveChangesAsync();
        var requests = db.Requests.OrderBy(r => r.Id).ToList();
        foreach (var index in new[] { 0, 1, 4 })
        {
            var request = requests[index];
            request.ApprovedAmount = request.EstimatedAmount;
            request.Status = index == 1 ? "Received" : "Approved";
            db.Approvals.Add(
                new ApprovalHistory
                {
                    PurchaseRequestId = request.Id,
                    Decision = "Approved",
                    Comment = "Demo management approval",
                    Amount = request.ApprovedAmount,
                    CreatedBy = admin.Id,
                }
            );
        }
        var invoice = new Invoice
        {
            PurchaseRequestId = requests[1].Id,
            SupplierId = requests[1].SupplierId!.Value,
            Number = "DEMO-FOOD-2026-01",
            Amount = requests[1].ApprovedAmount,
            CreatedBy = admin.Id,
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        db.Payments.Add(
            new Payment
            {
                PurchaseRequestId = requests[1].Id,
                InvoiceId = invoice.Id,
                Amount = 12500,
                Method = "M-Pesa",
                Reference = "DEMO-MPESA-001",
                Payee = "Dandora Food Distributors",
                ApprovedBy = admin.Id,
                CreatedBy = admin.Id,
            }
        );
        var maintenance = db.Departments.Single(d => d.Name == "Maintenance");
        var generator = db.Assets.Single(a => a.Code == "GEN-002");
        db.Expenses.Add(
            new Expense
            {
                DepartmentId = maintenance.Id,
                BudgetId = db.Budgets.First(b => b.DepartmentId == maintenance.Id).Id,
                Amount = 38000,
                Purpose = "Generator #2 annual servicing",
                ResponsiblePerson = "Maintenance Officer",
                Method = "Bank Transfer",
                Reference = "DEMO-SERVICE-001",
                Payee = "Eastlands Hardware & Electrical",
                AssetId = generator.Id,
                ApprovedBy = admin.Id,
                CreatedBy = admin.Id,
            }
        );
        var kitchen = db.Departments.Single(d => d.Name == "Kitchen");
        var account = db.PettyAccounts.First(a => a.DepartmentId == kitchen.Id);
        db.PettyTransactions.Add(
            new PettyTransaction
            {
                AccountId = account.Id,
                Kind = "Opening",
                Amount = 30000,
                Purpose = "Kitchen opening float",
                Person = "Kitchen Officer",
                Reference = "DEMO-FLOAT-001",
                ApprovedBy = admin.Id,
                CreatedBy = admin.Id,
            }
        );
        var books = new InventoryItem
        {
            Name = "Mathematics textbooks",
            Code = "BOOK-MATH",
            Category = "Learning Materials",
            LocationId = loc.Id,
            DepartmentId = db.Departments.Single(d => d.Name == "Teaching & Learning").Id,
            Unit = "books",
            ReorderLevel = 20,
        };
        db.Inventory.Add(books);
        await db.SaveChangesAsync();
        db.Income.Add(
            new Income
            {
                Source = "Church education ministry",
                Donor = "Mission Church Kenya",
                Type = "Non-cash Donation",
                InventoryItemId = books.Id,
                Quantity = 120,
                Purpose = "Learning resources",
                DepartmentId = books.DepartmentId,
            }
        );
        db.Movements.Add(
            new StockMovement
            {
                InventoryItemId = books.Id,
                DepartmentId = books.DepartmentId,
                Kind = "Donated",
                Quantity = 120,
                Source = "Church education ministry",
                CreatedBy = admin.Id,
            }
        );
        await db.SaveChangesAsync();
    }
}
