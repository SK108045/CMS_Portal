"""Live PostgreSQL acceptance suite. Run against an isolated DEMO database/server."""
import json, urllib.request,urllib.error,http.cookiejar,uuid,csv,io,os
BASE=os.environ.get('SCHOOL_URL','http://127.0.0.1:5000')+'/api/'
tag=uuid.uuid4().hex[:8]
class Client:
 def __init__(self):self.opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
 def call(self,path,method='GET',data=None,expect=200,raw=False):
  headers={}
  if method!='GET':headers['X-CSRF-TOKEN']=self.call('auth/csrf')['token']
  if data is not None:headers['Content-Type']='application/json';data=json.dumps(data).encode()
  req=urllib.request.Request(BASE+path,data=data,method=method,headers=headers)
  try:
   with self.opener.open(req) as r:status=r.status;body=r.read()
  except urllib.error.HTTPError as e:status=e.code;body=e.read()
  assert status==expect,(path,status,body.decode()[:1200])
  return body if raw else json.loads(body) if body else None
 def create(self,r,**d):return self.call('records/'+r,'POST',d)['id']
 def login(self,email,password):return self.call('auth/login','POST',dict(email=email,password=password))
 def upload(self,entity,id,kind='Receipt'):
  boundary='mission'+tag
  parts=[]
  for k,v in dict(entity=entity,entityId=id,kind=kind).items():parts.append(f'--{boundary}\r\nContent-Disposition: form-data; name="{k}"\r\n\r\n{v}\r\n'.encode())
  parts.append(f'--{boundary}\r\nContent-Disposition: form-data; name="file"; filename="evidence.pdf"\r\nContent-Type: application/pdf\r\n\r\n'.encode()+b'%PDF-1.4\n1 0 obj<</Type/Catalog>>endobj\n%%EOF\n'+f'\r\n--{boundary}--\r\n'.encode())
  req=urllib.request.Request(BASE+'documents',data=b''.join(parts),headers={'Content-Type':'multipart/form-data; boundary='+boundary,'X-CSRF-TOKEN':self.call('auth/csrf')['token']})
  with self.opener.open(req) as r:return json.load(r)['id']
a=Client();a.login('admin@missionschool.local','Admin123!')
dep=a.create('departments',name='Acceptance '+tag,head='Head Teacher',active=True)
uid=a.create('users',name='Acceptance Worker',email=tag+'@missionschool.local',departmentId=dep,position='Officer',role='USER',password='SchoolTest123!',active=True)
u=Client();u.login(tag+'@missionschool.local','SchoolTest123!');print('PASS 1: Admin creates department and worker; worker logs in')
category=a.call('records/categories')[0]['id'];supplier=a.call('records/suppliers')[0]['id']
budget=a.create('budgets',name='Acceptance allocation '+tag,departmentId=dep,categoryId=category,amount=100000,year='2026',startDate='2026-01-01',endDate='2026-12-31')
def summary():return next(b for b in a.call('records/budgets') if b['id']==budget)
assert next(b for b in u.call('records/budgets') if b['id']==budget)['remaining']==100000
print('PASS 2: Worker sees department budget')
def request():return u.create('requests',departmentId=dep,budgetId=budget,purpose='Test textbooks '+tag,items=[dict(description='Textbook',quantity=20,unitPrice=1000)])
def status(client,id,s,**kwargs):return client.call(f'workflows/requests/{id}/status','POST',dict(status=s,comment='Acceptance decision',amount=20000,supplierId=supplier,**kwargs))
r=request();status(u,r,'Submitted');status(a,r,'Approved');assert summary()['committed']==20000 and summary()['spent']==0
print('PASS 3: Approval commits funds without spending')
status(a,r,'Ordered');status(a,r,'Received');inv=a.create('invoices',purchaseRequestId=r,supplierId=supplier,number='INV-'+tag,amount=18000,date='2026-11-22');a.upload('invoices',inv,'Invoice')
def pay(amt,reference,final=False):return a.call('workflows/payments','POST',dict(purchaseRequestId=r,invoiceId=inv,amount=amt,method='M-Pesa',reference=reference,payee='School supplier',date='2026-11-22',overrideReason='',finalPayment=final))['id']
p1=pay(8000,'PART-'+tag);assert summary()['committed']==12000 and summary()['spent']==8000
p2=pay(10000,'FINAL-'+tag,True);proof=a.upload('payments',p2,'Payment Proof');status(a,r,'Completed');b=summary();assert (b['committed'],b['spent'],b['remaining'])==(0,18000,82000),b
assert len([h for h in a.call('records/audit') if h['entity']=='requests' and h['entityId']==r])>=7
print('PASS 4: Receipt, invoice, partial/final payment, evidence and audit; final payment releases unused approval')
r2=request();status(u,r2,'Submitted');status(a,r2,'Rejected');assert summary()['committed']==0
print('PASS 5: Rejected request reserves no funds')
account=a.create('petty-accounts',name='Acceptance imprest',departmentId=dep)
def petty(client,kind,amt):return client.create('petty-transactions',accountId=account,budgetId=budget,kind=kind,amount=amt,purpose='Cleaning supplies',person='Kitchen worker',reference=tag+'-'+kind,date='2026-11-22')
petty(a,'Opening',30000);pt=petty(u,'Expense',2300);assert next(x for x in a.call('records/petty-accounts') if x['id']==account)['balance']==27700;a.upload('petty-transactions',pt);petty(a,'Replenishment',2300);assert next(x for x in a.call('records/petty-accounts') if x['id']==account)['balance']==30000
print('PASS 6: Imprest spending, receipt and replenishment')
income=a.create('income',source='Mission church',donor='Church partner',type='Donation',amount=250000,purpose='School operations',reference='DON-'+tag,date='2026-11-22');assert any(i['id']==income for i in a.call('records/income'))
print('PASS 7: Financial donation recorded')
loc=a.call('records/locations')[0]['id'];item=a.create('inventory',name='Donated desks '+tag,code='TEST-'+tag,category='Permanent Items',unit='pieces',locationId=loc,departmentId=dep)
a.create('income',source='Mission partner',donor='School friends',type='Non-cash Donation',amount=0,quantity=25,inventoryItemId=item,purpose='New classroom',date='2026-11-22')
def quantity():return next(x for x in a.call('records/inventory') if x['id']==item)['quantity']
assert quantity()==25;print('PASS 8: Non-cash donation enters inventory')
a.create('movements',inventoryItemId=item,departmentId=dep,kind='Received',quantity=20,purchaseRequestId=None,source='Supplier',date='2026-11-22');a.create('movements',inventoryItemId=item,departmentId=dep,kind='Issued',quantity=5,source='Classroom issue',date='2026-11-22');assert quantity()==40
print('PASS 9: Stock receipt, issue and movement-derived quantity')
asset=a.create('assets',name='Generator test',code='GEN-'+tag,departmentId=dep,acquisitionCost=450000,location='Utility room');a.create('expenses',departmentId=dep,budgetId=budget,assetId=asset,amount=38000,purpose='Generator service',responsiblePerson='Maintenance Officer',method='Bank Transfer',reference='SERV-'+tag,payee='Engineer',date='2026-11-22');assert next(x for x in a.call('records/assets') if x['id']==asset)['lifetimeCost']==488000
print('PASS 10: Asset maintenance and lifetime cost')
report=a.call('insights/reports?name=Department+Expenditure&departmentId='+str(dep)+'&from=2026-11-01&to=2026-11-30');assert len(report)==1 and report[0]['amount']==38000
export=a.call('insights/reports?name=Department+Expenditure&departmentId='+str(dep)+'&csv=true',raw=True);assert len(list(csv.DictReader(io.StringIO(export.decode('utf-8-sig')))))==1
print('PASS 11: Filtered report and matching CSV')
u.call('records/users',expect=403);u.call('records/departments','POST',{'name':'Forbidden'},expect=403);other=Client();other.login('kitchen@missionschool.local','SchoolDemo123!');assert not any(x['id']==r for x in other.call('records/requests'));other.call('documents/'+str(proof),expect=403)
print('PASS 12: Admin endpoints and cross-department document/record access rejected')
u.call('records/petty-transactions','POST',dict(accountId=account,budgetId=budget,kind='Expense',amount=99999,purpose='Overdraw',person='Worker'),expect=400)
a.call('records/movements','POST',dict(inventoryItemId=item,kind='Issued',quantity=99999),expect=400)
a.call('workflows/payments','POST',dict(purchaseRequestId=r,invoiceId=inv,amount=1,method='Cash',reference='BAD-'+tag,payee='Supplier',date='2026-11-22',overrideReason='',finalPayment=False),expect=400)
print('PASS: Negative balance, excess stock issue and payment after completion rejected')
print('ALL LIVE ACCEPTANCE WORKFLOWS PASSED; run id '+tag)
# Additional lifecycle and concurrency-sensitive invariants.
r3=request();status(u,r3,'Submitted');status(a,r3,'Approved');assert summary()['committed']==20000;status(a,r3,'Cancelled');assert summary()['committed']==0
print('PASS: Cancellation releases an existing commitment')
asset_conversion=a.call(f'workflows/inventory/{item}/asset','POST',dict(departmentId=dep,code='CONVERT-'+tag,name='Classroom desk asset',location='Classroom 1',acquisitionCost=0,source='Donation'))['id'];assert quantity()==39;assert any(x['id']==asset_conversion for x in a.call('records/assets'))
print('PASS: Stock-to-asset conversion decrements stock and creates asset atomically')
# Safe scalar input binding discards nested graphs supplied by the caller.
nested=u.create('requests',departmentId=99999,budgetId=budget,purpose='Safe binding '+tag,budget={'id':budget,'amount':99999999},items=[dict(description='Textbook',quantity=1,unitPrice=100)])
assert next(x for x in u.call('records/requests') if x['id']==nested)['departmentId']==dep
assert summary()['amount']==100000
print('PASS: Client navigation graphs and department spoofing cannot change budgets')
tr=a.call('insights/reports?name=Transaction+Report&departmentId='+str(dep));tc=a.call('insights/reports?name=Transaction+Report&departmentId='+str(dep)+'&csv=true',raw=True);assert len(tr)==len(list(csv.DictReader(io.StringIO(tc.decode('utf-8-sig')))))
print('PASS: Unified filtered transaction report and CSV agree')
funding=u.create('petty-requests',accountId=account,amount=5000,purpose='Kitchen supplies imprest top-up')
u.call(f'workflows/petty-requests/{funding}/decision','POST',dict(approve=True,comment='Forbidden'),expect=403)
a.call(f'workflows/petty-requests/{funding}/decision','POST',dict(approve=True,comment='FUND-'+tag));assert next(x for x in a.call('records/petty-accounts') if x['id']==account)['balance']==35000
a.call(f'workflows/petty-requests/{funding}/decision','POST',dict(approve=True,comment='Duplicate approval'),expect=400)
print('PASS: Department funding request, Admin approval and single replenishment')
advance=petty(u,'Withdrawal',5000)
u.create('petty-transactions',accountId=account,budgetId=budget,kind='Expense',withdrawalId=advance,amount=3000,purpose='Advance spent',person='Kitchen worker',reference='SETTLE-'+tag)
u.create('petty-transactions',accountId=account,kind='Return',withdrawalId=advance,amount=2000,purpose='Unused cash returned',person='Kitchen worker',reference='RETURN-'+tag)
pa=next(x for x in a.call('records/petty-accounts') if x['id']==account);assert pa['balance']==32000 and pa['outstandingAdvances']==0,pa
u.call('records/petty-transactions','POST',dict(accountId=account,budgetId=budget,kind='Expense',withdrawalId=advance,amount=1,purpose='Oversettle',person='Worker'),expect=400)
print('PASS: Withdrawal settlement avoids double cash debit; unused cash return; excess settlement rejected')
