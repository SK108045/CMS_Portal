"""Run only against the disposable seeded verification server; exercises direct API boundaries."""
import uuid,json
from urllib.parse import urlencode
from http_client import Client
s=uuid.uuid4().hex[:8];pwd='PermissionTest123!'
a=Client();a.login('admin@missionschool.local','Admin123!')
d1=a.create('departments',name='Permissions A '+s);d2=a.create('departments',name='Permissions B '+s)
cat=a.call('records/categories')[0]['id'];supplier=a.call('records/suppliers')[0]['id'];location=a.call('records/locations')[0]['id']
b1=a.create('budgets',name='Permission budget A '+s,departmentId=d1,categoryId=cat,amount=100000,year='2026');b2=a.create('budgets',name='Permission budget B '+s,departmentId=d2,categoryId=cat,amount=200000,year='2026')
actors={};ids={};userbodies={}
for role,department in [('FINANCE',d2),('STOREKEEPER',None),('DEPARTMENT_HEAD',d1),('STAFF',d1),('AUDITOR',None)]:
 body=dict(name=role+' '+s,email=role.lower()+s+'@missionschool.local',role=role,departmentId=department,position='Test Officer',password=pwd,active=True)
 uid=a.create('users',**body);c=Client();profile=c.login(body['email'],pwd);assert profile['role']==role and profile['permissions'] and profile['access'];actors[role]=c;ids[role]=uid;userbodies[role]=body
f=actors['FINANCE'];store=actors['STOREKEEPER'];h=actors['DEPARTMENT_HEAD'];u=actors['STAFF'];aud=actors['AUDITOR']
for role in ['STAFF','DEPARTMENT_HEAD']:
 a.call('records/users','POST',dict(name='Missing department',email='missing'+role+s+'@missionschool.local',role=role,password=pwd),expect=400)
a.call('records/users','POST',dict(name='Bad role',email='bad'+s+'@missionschool.local',role='USER',departmentId=d1,password=pwd),expect=400)
assert {d1,d2}.issubset({b['departmentId'] for b in a.call('records/budgets')});print('PASS 1: Admin has all-school access; role and mandatory-department validation')
f.call('records/users',expect=403);f.call('records/users','POST',{},expect=403);f.call('records/users/'+str(ids['STAFF']),'PUT',userbodies['STAFF'],expect=403)
f.call('records/departments','POST',dict(name='Forbidden'),expect=403);f.call('records/departments/'+str(d1),'PUT',dict(name='Forbidden',active=False),expect=403)
print('PASS 3-4: Finance cannot manage users, roles or departments')
def request(c,budget,dept,purpose):return c.create('requests',departmentId=dept,budgetId=budget,purpose=purpose,items=[dict(description='Textbooks',quantity=2,unitPrice=500)])
r=request(u,b1,d2,'Staff request '+s);rh=request(h,b1,d2,'Head request '+s);foreign=request(a,b2,d2,'Other department '+s)
assert next(x for x in u.call('records/requests') if x['id']==r)['departmentId']==d1
assert rh not in {x['id'] for x in u.call('records/requests')};assert {r,rh}.issubset({x['id'] for x in h.call('records/requests')});assert foreign not in {x['id'] for x in h.call('records/requests?departmentId='+str(d2))}
h.call('records/requests/'+str(foreign),'PUT',dict(departmentId=d1,budgetId=b1,purpose='Spoof',items=[dict(description='x',quantity=1,unitPrice=1)]),expect=403)
h.call('records/requests','POST',dict(departmentId=d2,budgetId=b2,purpose='Spoof',items=[dict(description='x',quantity=1,unitPrice=1)]),expect=400)
for c in [u,h]:c.call('insights/reports?'+urlencode(dict(name='Budget vs Actual',departmentId=d2)),expect=403);assert all(x['departmentId']==d1 for x in c.call('records/budgets?departmentId='+str(d2)))
print('PASS 8-10,19: Department Head sees department requests; Staff sees own; spoofed departments and filters cannot cross scope')
def decision(c,rid,status,**kwargs):return c.call('workflows/requests/'+str(rid)+'/status','POST',dict(status=status,comment='Permission verification',amount=1000,supplierId=supplier,**kwargs))
decision(u,r,'Submitted')
for c in [u,h,f,store,aud]:c.call('workflows/requests/'+str(r)+'/status','POST',dict(status='Approved',comment='Forbidden',amount=1,supplierId=supplier),expect=403)
decision(a,r,'Approved');before=next(b for b in a.call('records/budgets') if b['id']==b1);assert before['committed']==1000 and before['spent']==0
print('PASS 11: Staff, Head, Finance, Stores and Auditor cannot perform final approval')
receiving=next(x for x in store.call('records/requests') if x['id']==r);assert all(k not in receiving for k in ['budgetId','estimatedAmount','approvedAmount','paid']);assert 'unitPrice' not in receiving['items'][0];assert foreign not in {x['id'] for x in store.call('records/requests')}
item=store.create('inventory',name='Permission stock '+s,code='PERM-'+s,unit='pieces',category='Permanent Items',locationId=location,departmentId=d1)
store.create('movements',inventoryItemId=item,departmentId=d1,kind='Received',quantity=2,unitCost=300,purchaseRequestId=r,source='Approved school purchase')
decision(store,r,'Received');assert next(x for x in a.call('records/requests') if x['id']==r)['approvedAmount']==1000
store.call('records/budgets','POST',{},expect=403);store.call('records/budgets/'+str(b1),'PUT',{},expect=403);store.call('records/expenses','POST',{},expect=403)
store.call('insights/reports?name=Supplier+Payments',expect=403);store.call('insights/charts/budgets',expect=403)
assert store.call('insights/reports?name=Stock+Movement+Report');print('PASS 5,7: Storekeeper receives stock; financial fields redacted; budgets, expenses and financial reports denied')
invoice=f.create('invoices',purchaseRequestId=r,supplierId=supplier,number='PERM-'+s,amount=800,date='2026-11-22')
def payload(amount,reference,final=False):return dict(purchaseRequestId=r,invoiceId=invoice,amount=amount,method='M-Pesa',reference=reference,payee='School supplier',date='2026-11-22',overrideReason='',finalPayment=final)
for c in [store,u,h,aud]:c.call('workflows/payments','POST',payload(300,'FORBIDDEN-'+s),expect=403)
payment=f.call('workflows/payments','POST',payload(300,'PART-'+s))['id'];assert next(b for b in a.call('records/budgets') if b['id']==b1)['committed']==700
f.call('workflows/payments','POST',payload(500,'FINAL-'+s,True));decision(f,r,'Completed')
paid=next(x for x in a.call('records/payments') if x['id']==payment);assert paid['createdBy']==ids['FINANCE'] and paid['approvedBy']!=ids['FINANCE'] and paid['createdAt']
after=next(b for b in a.call('records/budgets') if b['id']==b1);assert (after['committed'],after['spent'],after['remaining'])==(0,800,99200)
f.call('records/expenses','POST',dict(departmentId=d1,budgetId=b1,amount=200000,purpose='Over budget',responsiblePerson='Finance',method='Cash',reference='OVER-'+s,payee='Supplier',overrideReason='Cannot self-authorise'),expect=403)
print('PASS 2,6,12,21: Finance records partial/final payments; approver and recorder differ; commitments/spending correct; budget override denied')
financeDoc=f.upload('invoices',invoice,'Invoice');deliveryDoc=store.upload('requests',r,'Delivery Note');store.call('documents/'+str(financeDoc),expect=403);assert store.call('documents/'+str(deliveryDoc),raw=True).startswith(b'%PDF-');assert financeDoc not in {d['id'] for d in store.call('records/documents')}
foreignDoc=a.upload('requests',foreign,'Receipt');h.call('documents/'+str(foreignDoc),expect=403);u.call('documents/'+str(foreignDoc),expect=403);aud.call('documents/'+str(financeDoc),raw=True);aud.upload('requests',r,expect=403)
print('PASS: Evidence permissions combine linked entity, current department and document kind; Auditor cannot upload')
for resource in ['budgets','requests','approvals','expenses','suppliers','invoices','payments','petty-accounts','petty-transactions','petty-requests','income','inventory','movements','assets','documents','audit']:
 aud.call('records/'+resource);aud.call('records/'+resource,'POST',{},expect=403);aud.call('records/'+resource+'/1','PUT',{},expect=403)
assert {d1,d2}.issubset({x['departmentId'] for x in aud.call('records/budgets')});a.call('records/audit','POST',{},expect=403);a.call('records/audit/1','PUT',{},expect=403)
print('PASS 13-14,18: Auditor reads school-wide records; all generic writes forbidden; even Admin cannot mutate audit history')
# Cookie revocation on all relevant account changes.
body={k:v for k,v in userbodies['STAFF'].items() if k!='password'};a.call('records/users/'+str(ids['STAFF']),'PUT',{**body,'role':'DEPARTMENT_HEAD'});u.call('auth/me',expect=401);u.login(body['email'],pwd)
body={**body,'role':'DEPARTMENT_HEAD'};a.call('records/users/'+str(ids['STAFF']),'PUT',{**body,'departmentId':d2});u.call('auth/me',expect=401);u.login(body['email'],pwd)
a.call('records/users/'+str(ids['STAFF']),'PUT',{**body,'departmentId':d2,'active':False});u.call('auth/me',expect=401)
audit=a.call('records/audit');assert any(x['action']=='Role changed' and x['entityId']==ids['STAFF'] for x in audit);assert any(x['action']=='Department changed' and x['entityId']==ids['STAFF'] for x in audit);assert any(x['action']=='User deactivated' and x['entityId']==ids['STAFF'] for x in audit)
print('PASS 15-16: Role, department and deactivation revoke sessions and are audited')
# Finance may approve staff funding, never their own funding request.
account=f.create('petty-accounts',name='Permissions cash '+s,departmentId=d1);f.create('petty-transactions',accountId=account,kind='Opening',amount=5000,purpose='Opening',person='Custodian')
funding=h.create('petty-requests',accountId=account,amount=1000,purpose='Department needs');f.call('workflows/petty-requests/'+str(funding)+'/decision','POST',dict(approve=True,comment='Finance authorised funding'))
own=f.create('petty-requests',accountId=account,amount=1000,purpose='Self funding');f.call('workflows/petty-requests/'+str(own)+'/decision','POST',dict(approve=True,comment='Self approval'),expect=403)
h.call('records/petty-transactions','POST',dict(accountId=account,kind='Withdrawal',amount=100,purpose='Forbidden',person='Head'),expect=403)
print('PASS 22: Finance manages cash without self-approving funding; Head cannot become financial administrator')
asset=store.call('workflows/inventory/'+str(item)+'/asset','POST',dict(departmentId=d1,code='PERM-ASSET-'+s,name='Assigned stock',location='Classroom',acquisitionCost=999999,source='Purchase'))['id'];assert next(x for x in a.call('records/assets') if x['id']==asset)['acquisitionCost']==300
assert next(x for x in store.call('records/inventory') if x['id']==item)['quantity']==1
print('PASS 23: Stores conversion uses recorded cost, not client-supplied asset value; quantity maintained')
print('ALL PERMISSION ACCEPTANCE CHECKS PASSED '+s)
financial_history=f.call('insights/reports?'+urlencode(dict(name='User Activity Report',departmentId=d1)))
assert all(row['entity'] not in ['users','departments','system'] for row in financial_history)
print('PASS: Finance may filter school-wide financial history without receiving security administration logs')
r2=request(h,b1,d1,'Approval ceiling '+s);decision(h,r2,'Submitted');decision(a,r2,'Approved');decision(store,r2,'Received')
i2=f.create('invoices',purchaseRequestId=r2,supplierId=supplier,number='CEILING-'+s,amount=1500)
f.call('workflows/payments','POST',dict(purchaseRequestId=r2,invoiceId=i2,amount=1100,method='Cash',reference='CEILING-'+s,payee='School supplier',date='2026-11-22',overrideReason='Cannot grant own override',finalPayment=False),expect=403)
assert not any(p['purchaseRequestId']==r2 for p in a.call('records/payments'))
print('PASS: Finance cannot exceed an approval even when the budget has ample money')
headEvidence=h.upload('inventory',item,'Receipt')
store.call('records/inventory/'+str(item),'PUT',dict(name='Permission stock '+s,code='PERM-'+s,unit='pieces',category='Permanent Items',locationId=location,departmentId=d2))
h.call('documents/'+str(headEvidence),expect=403)
print('PASS: Moving a linked entity revokes former-department document access even if document metadata is stale')
assert f.call('insights/reports?name=Purchase+Requests')
print('PASS: Finance can report on school-wide purchasing without receiving stock-management permission')
