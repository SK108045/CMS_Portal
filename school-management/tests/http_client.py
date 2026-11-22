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
 def upload(self,entity,id,kind='Receipt',expect=200):
  boundary='mission'+tag
  parts=[]
  for k,v in dict(entity=entity,entityId=id,kind=kind).items():parts.append(f'--{boundary}\r\nContent-Disposition: form-data; name="{k}"\r\n\r\n{v}\r\n'.encode())
  parts.append(f'--{boundary}\r\nContent-Disposition: form-data; name="file"; filename="evidence.pdf"\r\nContent-Type: application/pdf\r\n\r\n'.encode()+b'%PDF-1.4\n1 0 obj<</Type/Catalog>>endobj\n%%EOF\n'+f'\r\n--{boundary}--\r\n'.encode())
  req=urllib.request.Request(BASE+'documents',data=b''.join(parts),headers={'Content-Type':'multipart/form-data; boundary='+boundary,'X-CSRF-TOKEN':self.call('auth/csrf')['token']})
  try:
   with self.opener.open(req) as r:status=r.status;body=r.read()
  except urllib.error.HTTPError as e:status=e.code;body=e.read()
  assert status==expect,(entity,status,body.decode()[:1000])
  return json.loads(body)['id'] if status==200 else None
