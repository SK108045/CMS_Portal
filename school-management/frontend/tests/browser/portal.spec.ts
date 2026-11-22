import { test, expect } from "@playwright/test";
test("Admin portal: live dashboard, forms, all pages and reports", async ({
  page,
}) => {
  const errors: string[] = [];
  page.on("pageerror", (e) => errors.push(e.message));
  await page.goto("/");
  await page
    .getByLabel("Email", { exact: true })
    .fill("admin@missionschool.local");
  await page.getByLabel("Password", { exact: true }).fill("Admin123!");
  await page.getByRole("button", { name: "Sign in →" }).click();
  await expect(
    page.getByRole("heading", { name: "Every shilling, accounted for." }),
  ).toBeVisible();
  await expect(page.getByText("Total budget", { exact: true })).toBeVisible();
  await page.screenshot({ path: "../tests/dashboard.png", fullPage: true });
  await page.getByRole("link", { name: "Departments", exact: true }).click();
  await page.getByRole("button", { name: "+ New record" }).click();
  const name = "Browser Department " + Date.now();
  await page.getByLabel("Department name").fill(name);
  await page.getByLabel("Person responsible").fill("Head Teacher");
  await page.getByRole("button", { name: "Save record", exact: true }).click();
  await page.getByPlaceholder("Search records…").fill(name);
  await expect(page.getByRole("cell", { name, exact: true })).toBeVisible();
  for (const path of [
    "users",
    "budgets",
    "requests",
    "approvals",
    "expenses",
    "suppliers",
    "payments",
    "petty-accounts",
    "income",
    "inventory",
    "assets",
    "documents",
    "audit",
    "settings",
    "invoices",
    "adjustments",
    "categories",
    "locations",
    "movements",
    "petty-transactions",
  ]) {
    await page.goto("/#" + path);
    await expect(page.locator("h1")).toBeVisible();
    await expect(page.getByText("Loading records…")).toHaveCount(0);
    await expect(page.locator('[role="alert"]')).toHaveCount(0);
  }
  await page.goto("/#reports");
  await page
    .getByRole("combobox", { name: "Report", exact: true })
    .selectOption("Department Expenditure");
  await expect(page.getByText("Preparing report…")).toHaveCount(0);
  const download = page.waitForEvent("download");
  await page.getByRole("link", { name: "Export CSV" }).click();
  expect((await download).suggestedFilename()).toContain(".csv");
  expect(errors).toEqual([]);
});
test("Department portal creates and submits a real request", async ({
  page,
}) => {
  await page.goto("/");
  await page
    .getByLabel("Email", { exact: true })
    .fill("kitchen@missionschool.local");
  await page.getByLabel("Password", { exact: true }).fill("SchoolDemo123!");
  await page.getByRole("button", { name: "Sign in →" }).click();
  await expect(
    page.getByRole("heading", { name: "Every shilling, accounted for." }),
  ).toBeVisible();
  await expect(
    page.getByRole("link", { name: "Users", exact: true }),
  ).toHaveCount(0);
  await page
    .getByRole("link", { name: "Purchase Requests", exact: true })
    .click();
  await page.getByRole("button", { name: "+ New request" }).click();
  const purpose = "Browser kitchen supplies " + Date.now();
  await page.getByLabel("Budget allocation").selectOption({ index: 1 });
  await page.getByLabel("Purpose", { exact: false }).first().fill(purpose);
  await page
    .getByLabel("Item description", { exact: true })
    .fill("Cooking oil");
  await page.getByLabel("Quantity", { exact: true }).fill("5");
  await page.getByLabel("Unit price in KES", { exact: true }).fill("1200");
  await page.getByRole("button", { name: "Save record", exact: true }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await page.getByPlaceholder("Search records…").fill(purpose);
  await expect(page.locator("tbody tr")).toHaveCount(1);
  await page.getByRole("button", { name: "Open ↗" }).click();
  await page.getByLabel("Next stage").selectOption("Submitted");
  await page.getByRole("button", { name: "Confirm decision" }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await expect(page.locator("tbody").first()).toContainText("Submitted");
});

test("Petty cash funding request is approved through the browser", async ({
  page,
}) => {
  await page.goto("/");
  await page
    .getByLabel("Email", { exact: true })
    .fill("kitchen@missionschool.local");
  await page.getByLabel("Password", { exact: true }).fill("SchoolDemo123!");
  await page.getByRole("button", { name: "Sign in →" }).click();
  await expect(
    page.getByRole("heading", { name: "Every shilling, accounted for." }),
  ).toBeVisible();
  await page.goto("/#petty-requests");
  await page.getByRole("button", { name: "+ New record" }).click();
  await page.getByLabel("Petty cash account").selectOption({ index: 1 });
  await page.getByLabel("Amount (KES)").fill("5000");
  const purpose = "Browser imprest " + Date.now();
  await page.getByLabel("Purpose").fill(purpose);
  await page.getByRole("button", { name: "Save record", exact: true }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await page.getByPlaceholder("Search records…").fill(purpose);
  await expect(page.locator("tbody").first()).toContainText("Pending");
  await page.getByRole("button", { name: "Sign out", exact: true }).click();
  await page
    .getByRole("dialog")
    .getByRole("button", { name: "Sign out", exact: true })
    .click();
  await page
    .getByLabel("Email", { exact: true })
    .fill("admin@missionschool.local");
  await page.getByLabel("Password", { exact: true }).fill("Admin123!");
  await page.getByRole("button", { name: "Sign in →" }).click();
  await expect(
    page.getByRole("heading", { name: "Every shilling, accounted for." }),
  ).toBeVisible();
  await page.goto("/#petty-requests");
  await page.getByPlaceholder("Search records…").fill(purpose);
  await expect(page.locator("tbody tr")).toHaveCount(1);
  await page.getByRole("button", { name: "Open ↗" }).click();
  await page
    .getByLabel("Decision comments / cash issue reference")
    .fill("Approved top-up for kitchen");
  await page.getByRole("button", { name: "Approve & replenish" }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await expect(page.locator("tbody").first()).toContainText("Approved");
});
