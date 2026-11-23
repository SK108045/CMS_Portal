import { test, expect, APIRequestContext, Page } from "@playwright/test";

async function post(client: APIRequestContext, path: string, data: object) {
  const token = (await (await client.get("/api/auth/csrf")).json()).token;
  const response = await client.post("/api/" + path, {
    headers: { "X-CSRF-TOKEN": token },
    data,
  });
  expect(response.ok(), await response.text()).toBeTruthy();
  const body = await response.text();
  return body ? JSON.parse(body) : null;
}
async function login(
  page: Page,
  email = "admin@missionschool.local",
  password = "Admin123!",
) {
  await page.goto("/");
  await page.getByLabel("Email", { exact: true }).fill(email);
  await page.getByLabel("Password", { exact: true }).fill(password);
  await page.getByRole("button", { name: "Sign in →" }).click();
  await expect(
    page.getByRole("heading", {
      level: 1,
      name: /Approve purchase requests|Every shilling/,
    }),
  ).toBeVisible();
}
async function fixture(
  client: APIRequestContext,
  options: {
    self?: boolean;
    missingSupplier?: boolean;
    amount?: number;
    draft?: boolean;
  } = {},
) {
  await post(client, "auth/login", {
    email: "admin@missionschool.local",
    password: "Admin123!",
  });
  const departments = await (
    await client.get("/api/records/departments")
  ).json();
  const departmentId = departments.find((d: any) => d.name === "Kitchen").id;
  const categoryId = (
    await (await client.get("/api/records/categories")).json()
  )[0].id;
  const supplierId = (
    await (await client.get("/api/records/suppliers")).json()
  ).find((s: any) => s.active).id;
  const tag = Date.now() + "-" + Math.random().toString(36).slice(2, 7);
  const budgetId = (
    await post(client, "records/budgets", {
      name: "Approval inbox test " + tag,
      departmentId,
      categoryId,
      amount: 10000,
      year: "2026",
      startDate: "2026-01-01",
      endDate: "2026-12-31",
    })
  ).id;
  if (!options.self) {
    await post(client, "auth/logout", {});
    await post(client, "auth/login", {
      email: "kitchen@missionschool.local",
      password: "SchoolDemo123!",
    });
  }
  const purpose = "Inbox decision " + tag;
  const id = (
    await post(client, "records/requests", {
      departmentId,
      budgetId,
      purpose,
      priority: "Urgent",
      requiredBy: "2000-01-01",
      supplierId: options.missingSupplier ? null : supplierId,
      items: [
        {
          description: "School supplies",
          quantity: 1,
          unitPrice: options.amount ?? 1200,
        },
      ],
    })
  ).id;
  if (!options.draft)
    await post(client, `workflows/requests/${id}/status`, {
      status: "Submitted",
      comment: "Ready for school management",
      amount: null,
      supplierId: null,
    });
  return { id, purpose, budgetId, supplierId };
}
async function budget(page: Page, id: number) {
  return (await (await page.request.get("/api/records/budgets")).json()).find(
    (b: any) => b.id === id,
  );
}

test("Home puts real pending requests before charts and approves with one click", async ({
  page,
  request,
}) => {
  const f = await fixture(request);
  await login(page);
  const card = page.getByRole("article", { name: f.purpose, exact: true });
  await expect(card).toBeVisible();
  const box = await card
    .getByRole("button", { name: "Approve", exact: true })
    .boundingBox();
  expect(box!.y).toBeLessThan(720);
  const inboxBox = await page
    .getByRole("region", { name: "Purchase request approvals" })
    .boundingBox();
  const metricsBox = await page.locator(".cards").first().boundingBox();
  expect(inboxBox!.y).toBeLessThan(metricsBox!.y);
  await card.getByRole("button", { name: "Approve", exact: true }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await expect(card).toHaveCount(0);
  const b = await budget(page, f.budgetId);
  expect([b.committed, b.spent, b.remaining]).toEqual([1200, 0, 8800]);
  await expect(page.getByRole("status")).toContainText("approved");
  const history = (
    await (await page.request.get("/api/records/approvals")).json()
  ).filter((h: any) => h.purchaseRequestId === f.id);
  expect(history.some((h: any) => h.decision === "Approved")).toBeTruthy();
  await page.screenshot({
    path: "/tmp/mission-approval-home.png",
    fullPage: false,
  });
});

for (const decision of ["Reject", "Return"]) {
  test(`${decision} from inbox requires a reason and reserves no funds`, async ({
    page,
    request,
  }) => {
    const f = await fixture(request);
    await login(page);
    await page
      .getByRole("article", { name: f.purpose, exact: true })
      .getByRole("button", { name: decision, exact: true })
      .click();
    const dialog = page.getByRole("dialog");
    await expect(
      dialog.getByLabel("Decision / override reason"),
    ).toHaveAttribute("required", "");
    await dialog
      .getByLabel("Decision / override reason")
      .fill("Please provide the updated quotation.");
    await dialog
      .getByRole("button", { name: decision + " request", exact: true })
      .click();
    await expect(dialog).toHaveCount(0);
    const r = (
      await (await page.request.get("/api/records/requests")).json()
    ).find((r: any) => r.id === f.id);
    expect(r.status).toBe(
      decision === "Reject" ? "Rejected" : "Returned for Changes",
    );
    expect((await budget(page, f.budgetId)).committed).toBe(0);
  });
}

test("Missing supplier opens focused review and can adjust approved amount", async ({
  page,
  request,
}) => {
  const f = await fixture(request, { missingSupplier: true });
  await login(page);
  await page
    .getByRole("article", { name: f.purpose, exact: true })
    .getByRole("button", { name: "Approve", exact: true })
    .click();
  const dialog = page.getByRole("dialog");
  await expect(dialog.getByLabel("Supplier", { exact: true })).toHaveValue("");
  await dialog
    .getByLabel("Supplier", { exact: true })
    .selectOption(String(f.supplierId));
  await dialog.getByLabel("Approved amount (KES)").fill("1000");
  await dialog
    .getByRole("button", { name: "Approve request", exact: true })
    .click();
  await expect(dialog).toHaveCount(0);
  expect((await budget(page, f.budgetId)).committed).toBe(1000);
});

for (const variant of ["self", "overbudget"]) {
  test(`${variant} approval requires and audits an override reason`, async ({
    page,
    request,
  }) => {
    const f = await fixture(
      request,
      variant === "self" ? { self: true } : { amount: 12000 },
    );
    await login(page);
    await page
      .getByRole("article", { name: f.purpose, exact: true })
      .getByRole("button", { name: "Approve", exact: true })
      .click();
    const dialog = page.getByRole("dialog");
    await expect(
      dialog.getByLabel("Decision / override reason"),
    ).toHaveAttribute("required", "");
    await dialog
      .getByLabel("Decision / override reason")
      .fill(
        "Urgent authorised school requirement; owner takes responsibility.",
      );
    await dialog
      .getByRole("button", { name: "Approve request", exact: true })
      .click();
    await expect(dialog).toHaveCount(0);
    const audit = await (await page.request.get("/api/records/audit")).json();
    expect(
      audit.some(
        (a: any) =>
          a.entityId === (variant === "self" ? f.id : f.budgetId) &&
          a.action ===
            (variant === "self" ? "Self-approval override" : "Budget override"),
      ),
    ).toBeTruthy();
  });
}

test("Drafts have a visible submit action and enter the decision queue only after submission", async ({
  page,
  request,
}) => {
  const f = await fixture(request, { draft: true });
  await login(page);
  let queue = await (
    await page.request.get("/api/insights/approval-queue")
  ).json();
  expect(queue.items.some((r: any) => r.id === f.id)).toBeFalsy();
  await page.goto("/#requests?status=Draft");
  await page.getByPlaceholder("Search records…").fill(f.purpose);
  await page
    .locator(".table-panel tbody tr")
    .getByRole("button", { name: "Submit for approval", exact: true })
    .click();
  await expect(
    page.getByRole("article", { name: f.purpose, exact: true }),
  ).toBeVisible();
  queue = await (await page.request.get("/api/insights/approval-queue")).json();
  expect(queue.items.some((r: any) => r.id === f.id)).toBeTruthy();
  await page.goto("/#approvals");
  await page.getByLabel("Search approval inbox").fill(f.purpose);
  await page
    .getByRole("article", { name: f.purpose, exact: true })
    .getByRole("button", { name: "Approve", exact: true })
    .click();
  await expect(
    page.getByRole("article", { name: f.purpose, exact: true }),
  ).toHaveCount(0);
});

for (const role of ["finance", "storekeeper", "head", "kitchen", "auditor"]) {
  test(`${role} has no approval inbox and direct access is forbidden`, async ({
    page,
  }) => {
    await login(page, `${role}@missionschool.local`, "SchoolDemo123!");
    await expect(
      page.getByRole("region", { name: "Purchase request approvals" }),
    ).toHaveCount(0);
    expect(
      (await page.request.get("/api/insights/approval-queue")).status(),
    ).toBe(403);
  });
}

test("A stale one-click approval cannot silently override a newly exhausted budget", async ({
  page,
  request,
}) => {
  const f = await fixture(request);
  await login(page);
  const card = page.getByRole("article", { name: f.purpose, exact: true });
  await expect(card).toBeVisible();
  const me = await (await request.get("/api/auth/me")).json();
  await post(page.request, "records/expenses", {
    departmentId: me.departmentId,
    budgetId: f.budgetId,
    amount: 9000,
    purpose: "Concurrent authorised utility bill",
    responsiblePerson: "Bursar",
    method: "Cash",
    reference: "STALE-" + f.id,
    payee: "Utility provider",
    date: "2026-11-23",
  });
  await card.getByRole("button", { name: "Approve", exact: true }).click();
  await expect(page.getByRole("alert")).toContainText("override reason");
  const b = await budget(page, f.budgetId);
  expect([b.committed, b.spent, b.remaining]).toEqual([0, 9000, 1000]);
  await expect(card).toContainText("A budget override reason is required");
  await card.getByRole("button", { name: "Approve", exact: true }).click();
  const dialog = page.getByRole("dialog");
  await expect(dialog.getByLabel("Decision / override reason")).toHaveAttribute(
    "required",
    "",
  );
  await dialog
    .getByLabel("Decision / override reason")
    .fill(
      "Owner authorises this essential school supply despite the intervening utility expense.",
    );
  await dialog
    .getByRole("button", { name: "Approve request", exact: true })
    .click();
  await expect(dialog).toHaveCount(0);
  expect((await budget(page, f.budgetId)).committed).toBe(1200);
});
