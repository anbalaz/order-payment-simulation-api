# AI Workflow Dokumentácia

**Meno:** 

**Dátum začiatku:** 

**Dátum dokončenia:** 

**Zadanie:** Frontend / Backend

---

## 1. Použité AI Nástroje

Vyplň približný čas strávený s každým nástrojom:

- [ ] **Rider IDE:** 1 hodín
- [ ] **Claude Code:** 5 hodín

**Celkový čas vývoja (priližne):** 12 hodín

---

## 2. Zbierka Promptov

> 💡 **Tip:** Kopíruj presný text promptu! Priebežne dopĺňaj po každej feature.

### Prompt #1: [Inicializacia postgres db s tabulkami- fungovala viac menej len par zadrhelov s nastavenim projektu]
## FEATURE:

**Nástroj:** [ Claude Code ]
**Kontext:** [ Setup dbs]

**Prompt:**
```
Initialize Postgres in docker

I have docker desktop app. Run Postgres in docker and initialize it with docker compose file. Include compose file in repository, store it in Folder Postgres. Database should hold data about orders, users, payments, so choose good name for it.
Add mcp server to Postgres and connect to it, add this settings to repository, so you can use it.

In postgres should be these tables:

Users:
id
name max length 100,
email max length 100 and unique,
password string (should be hashed and protected like passwords are in db, so nobody can decipher them).

Products:
id, 
name max length 100,
description string,
price number >=0,
created_at timestamp

Orders:
id, 
user_id,
total number >=0, 
status (should be enum, in db store it as tinyInt or similar type),
items schema id (primary key),
product_id,
quantity (number>0)
price (number>0)
created_at timestamp
updated_at timestamp

In orders user_id is id from Users table and product_id is reference to id from Products table.

Include in DBS also initial seed data for tables. These scripts tore in Postgres folder.

Include into the final solution DB upgrade mechanism. It has to contain some form of upgrade
DB scripts or DB upgrade code.
```

**Výsledok:**
⭐⭐⭐⭐ Dobré, potreboval malé úpravy

**Čo som musel upraviť / opraviť:**
```
zabudol som este na Readme, musel som claude poziadat o pridanie: Also in INITIAL.md add step for new file Readme.md in root of project and document how to run DB upgrade tool and how to start the service.
Prejekt aj db fungovali, ale nenaseedoval data do db.

Musel som sa ho spytat na : can you read data from postgres users table? what are first 2 users data? 
```

**Poznámky / Learnings:**
```
Mal som lepsie specifikovat, za akych okolnosti je vsetko ok. Mal som mu zadat, nech skontroluje, ci vidi data v db. Taktiez som mu mohol lepsie specifikovat style, ake nugety preferujem ale som s nim zatial v pohode.
v commande execute prp som zadal zly prompt na path ku projektu, musel som rucne opravit. Musim po napisani skontrolovat ci commandy na kontrolu vobec funguju!
```


### Prompt #2: [Upravit nazov stlpca v db]

**Nástroj:** [Claude code]
**Kontext:** [po dokonceni mojho prp som potreboval este upravit nazov stlpca a skontrolovat ci vsetko funguje]

**Prompt:**
```
it works, thanks. In the table User I see you used column name password_hash instead of password. Please rename it and also renami it in C# model. Don't     
forget to fix it in seeded data. After doing it test if data in db are seeded and if migration works.
```

**Výsledok:**  
✅ Fungoval perfektne (first try)

**Úpravy:**
```
```

**Poznámky:**
```
```


### Prompt #3: [Update Claude.md]

**Nástroj:** [ Claude Code]   
**Kontext:** [update claude po velkej feature]

**Prompt:**
```
update Claude.md 
```

**Výsledok:**  
✅ Fungoval perfektne (first try)

**Úpravy:**
```
```

**Poznámky:**
```
```

### Prompt #4: [Pridanie controllera pre Usera]

**Nástroj:** [ Claude Code]   
**Kontext:** [update claude po velkej feature]

**Prompt:**
```
## FEATURE:

New controller for User. That would support CRUD operations. Model of user: id, name (max length 100), email (unique), password.
Controller Should have 4 endpoints, 

PUT api/user (create)
POST api/user (update),
GET api/user (get),
DELETE api/user (delete).

Http responses for user
201 for created (PUT)
Validate inputs if not valid return 400. If valid create/update/get/delete data in db if the endpoint requires it.
401 for unauthorized (when no jwt token or token that has no right over user)
200 OK for get, returns data about user (Id, name, email, createdAt, updatedAt)
500 if unexpected error occurs.
add other if you consider it necessary

New authentication controller for Login, should follow REST API
checks user credentials (email, password) and if correct, return JWT Token

for invalid credentials in login return 401

add integration tests into new IntegrationTests project that is in new folder test in root of the project example (.\test\IntegrationTests\IntegrationTests.csproj). 
if necessary add unit tests into new UnitTests project that is in new folder test in root of the project example (.\test\UnitTests\UnitTests.csproj). 

For tests use x-unit tests and also autofixture (https://www.nuget.org/packages/autofixture), Moq (https://www.nuget.org/packages/moq/)

update Readme about new features.

Remove weather controller with all its linked structure and data, It is no longer needed.

At the end check if data in postgre db is changed when you use endpoints accordingly

after everything works update Claude.md

## EXAMPLES:

model for stored data for user should be in .src/OrderPaymentSimulation.Api/Models/User.

User in Models folder: 
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

UserDto in Dtos folder:
public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

For mapping between models do manually, classes should have static models, for example
public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static UserDto CreateFrom (User user)
      =>new ()
      {
          Name= user.Name
          Email= user.Email,
          Password= user.Password
          CreatedAt= user.CreatedAt
          UpdatedAt= user.UpdatedAt
      }
}


## DOCUMENTATION:

to check data and format look into mcp server for postgres tables

## OTHER CONSIDERATIONS:

Use Action result from Microsoft.AspNetCore.Mvc, for wrapping response body

For handling authentication use Microsoft.IdentityModel.JsonWebTokens. Users should be stored in db that already exists. You should not use same model for controller and db. Create separate Dtos folder where copy of User will be. Named UserDto.

All Endpoints should be protected with JWT Bearer Token (except Login endpoint and creating user). Result of Rest API

use swagger for api documentation

```

**Výsledok:**  
✅ Fungoval perfektne (first try)

**Úpravy:**
```
```

**Poznámky:**
```
```


### Prompt #5: [Merge changes]

**Nástroj:** [ Claude Code]   
**Kontext:** [robil som upravy na branchy, ktora uz bola mergnuta]

**Prompt:**
```
merge changes
```

**Výsledok:**  
⭐⭐⭐⭐ Dobré, potreboval malé úpravy

**Úpravy:**
```
 when running locally app I get from swagger: No operations defined in spec (originally I posted a picture)
```

**Poznámky:**
```
vagne som naformuloval prompt :), lenivo
```


### Prompt #6: [Change Project structure]

**Nástroj:** [ Claude Code]   
**Kontext:** [fixing wrong architecture of project that was cause by my doing]

**Prompt:**
```
 update project structure, I don't want the OrderPaymentSimulation.Api.csproj be nested in src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api. There is unnecessary one folder of OrderPaymentSimulation.Api nesting. Fix it and for all other files as well. Project and tests should be runnable
```

**Výsledok:**  
✅ Fungoval perfektne (first try) 

**Úpravy:**
```

```

**Poznámky:**
```

```


### Prompt #6: [Add Products and Orders controllers]

**Nástroj:** [ Claude Code]   
**Kontext:** [pridanie controllerov pre orders and products]

**Prompt:**
```
New controller for Products. Product has following fields: id, name string max length 100, description string, price number >=
0, stock number >= 0, created_at timestamp.
Validate input DTOs. If wrong return 400

new controller for Orders. Order has following fields: id, user_id , total number >= 0, status enum (pending, processing,
completed, expired), items schema id primary key, product_id, quantity number > 0, price
number > 0 created_at timestamp, updated_at timestamp
Create CRUD REST API for this module
Validate input DTOs. The rules are in scheme


Controllers Should have 4 endpoints, 

inspire the responses and status codes from @UsersCcntroller

add additional Http status codes if you consider it necessary

add integration tests into new IntegrationTests project that is in new folder test in root of the project example (.\test\IntegrationTests\IntegrationTests.csproj). 
if necessary add unit tests into new UnitTests project that is in new folder test in root of the project example (.\test\UnitTests\UnitTests.csproj). 

For tests use x-unit tests and also autofixture (https://www.nuget.org/packages/autofixture), Moq (https://www.nuget.org/packages/moq/)

update Readme about new features.

At the end check if data in postgres db is changed when you use endpoints accordingly

after everything works update Claude.md

## EXAMPLES:

inspire how db model for user is in folder   .src/OrderPaymentSimulation.Api/Models/User.
and also its controller model is in .src/OrderPaymentSimulation.Api/Dtos folder

for mapping between models use the example in UserDto method CreateFrom

## DOCUMENTATION:

to check data and format look into mcp server for postgres tables

## OTHER CONSIDERATIONS:

Use Action result from Microsoft.AspNetCore.Mvc, for wrapping response body

For handling authentication use Microsoft.IdentityModel.JsonWebTokens. Products should be stored in db that already exists. You should not use same model for controller and db. 

All new Endpoints should be protected with JWT Bearer Token

use swagger for api documentation

```

**Výsledok:**  
⭐⭐⭐ OK, potreboval viac úprav  

**Úpravy:**
```
claude nezhodil db a nenaseedoval nove data, kedze upravoval db schemu. Musel som zadat command po mojom teste: 
you have added new controllers and logic for orders and products. When I am trying to  get every products by endpoint GET:api/product I am getting 500 http status and error message 'An error occurred while retrieving products' [Image #1] 

 Taktiez zle zadefinoval http metody PUT and POST, ked som ich nevymenoval, opravene po prompte:
 when creating order, I am getting 'An error occured while creating order' [Image #1]. Please fix it, also check all endpoints if they work correctly and don't throw unexpected error. I have also noticed that you switched POST and PUT method in product and order controller. Post should be used for update and PUT for create

```

**Poznámky:**
```
treba ho upozornit na to, aby naseedoval na novo db po zmenach v db
presnejsie definovat, alebo zamerat sa, aby PUT/ POST nezamienal
```

### Prompt #6: [Orders workflow]

**Nástroj:** [ Claude Code]   
**Kontext:** [Kafka and order workflows]

**Prompt:**
```

Initialize Kafka in docker

I have docker desktop app. Run Kafka in docker and Update existing docker-compose file so this service can be created inside docker. Include kafka into compose file, stored in Folder Postgres.
Add mcp server and connect to it, add this settings to repository, so you can use it.

move existing ./Postgres/docker-compose.yml file to root of project.

add Kafka into the project, so the messages/events can be sent/published as transport system.
---------------------------------------------
How the kafka should work:

When the order is created and stored in db with status 'pending', the 'OrderCreated' event has to be published

OrderCreatedHandler (asynchronous) should handle this event and :
    -handles 'OrderCreated' events
    -Update order status: to 'processing' (update it in db)
    -simulate payment processing (5 second delay)
    -after this 5 sec simulation Update order status for 50% of cases to 'completed' and publish 'OrderCompleted' event
    -In another 50% of cases do not change the status, it remains as 'processing'
----------------------------------------------
Order expiration handling:

    - Add hosted backgroundService that runs every 60 seconds, gets all orders older than 10 minutes and have status 'processing' and updates it's status to 'expired'
    - after updating publish 'OrderExpired' event
----------------------------------------------
Add new notification table into postgres db, update also seed script
table should have reference at least to user and order, should also have field for notification and also if the order was completed or expired.

Notification handler (asynchronous)
    - handles 'OrderCompleted' and 'OrderExpired' events
    - when 'OrderCompleted' event is published, log fake email notification into console (something like: order number 1234 for user user@email.com was issued containing products :...). Save notification to 'notification' table in database (as an audit trail)
    - when 'OrderExpired' event is published, save notification to database (audit trail)

-----------------------------------------------
Expected Flow:
1. User creates order via POST /api/orders
2. Order saved to DB with status='pending'
3. OrderCreated event published
4. OrderProcessor handles event asynchronously:
  -Updates status to 'processing'
  -Simulates payment (5 sec delay)
  -Updates status to 'completed'
5. OrderCompleted event published
6. Notifier handles event:
  -Logs fake email to console
  -Saves notification to DB
7. CRON job runs every 60s:
  -Finds pending orders older than 10 minutes
  -Updates them to 'expired
```

**Výsledok:**  
⭐⭐⭐⭐ Dobré, potreboval malé úpravy

**Úpravy:**
```
Claude po sebe neupratal nechal beziace instancie.

ok close all usings of project 

nepomohlo :D, zle formulovane, musel som dat novy command:

Did you? I am getting: Warning MSB3026 : Could not copy "C:\Mine\order-payment-simulation-api\src\OrderPaymentSimulation.Api\obj\Debug\net8.0\apphost.exe" to "bin\Debug\net8.0\OrderPaymentSimulation.Api.exe". Beginning retry 1 in 1000ms. The process cannot access the file 
'C:\Mine\order-payment-simulation-api\src\OrderPaymentSimulation.Api\bin\Debug\net8.0\OrderPaymentSimulation.Api.exe' because it is being used by another process. The file is locked by: "OrderPaymentSimulation.Api (27696)"
```

**Poznámky:**
```
po sebe by si mal vsetko starostlivo upratat, nechava spustene programi a instancie. Musim mu to pri instrukciach povedat.
```

### Prompt #6: [Used my own commands]

**Nástroj:** [ Claude Code]   
**Kontext:** [ Used command /pr to create PR and after that I tried to fix it with prompt]

**Prompt:**
```
 I asked you to use /pr command and inside it you used create-branch command. You created branch feature/kafka-event-driven-order-processing. In the command there is insturction to use snake case for naming branch, but you used kebab case. Please move those changes you commit to branch 'feature/kafka-event-driven-order-processing' to another branch that comply to my commamnds.     
Also Update the commands, so you would not use kebab case in future, also remove any mention of Target Process. I also want to have option besides US number to pass argument if the changes are feature of hotfix and etc.
```

**Výsledok:**  
⭐⭐⭐⭐ Dobré, potreboval malé úpravy

**Úpravy:**
```
Urobil upravy v commande, ale pridal tam backticks, s ktorymi to nefunguje,
tak som ho v ramci jeho otazky, ci je vsetko ok napisal: 

please also remove backticks from commands you added, because with those command does not work

```

**Poznámky:**
```
Ocakaval som, ze nezahodi staru branchu, ale zahodil, niekedy sa to chova nekonzistentne, teraz po sebe upratal, ale pri predchadzajucom prompte za sebou nechal binec
```



---

## 3. Problémy a Riešenia 

> 💡 **Tip:** Problémy sú cenné! Ukazujú ako riešiš problémy s AI.

### Problém #1: Nevytvoril spravne endpointy pre Orders/Products

**Čo sa stalo:**
```
[povedal som mu aby vytvoril crud API endpointy pre produkty a ordery, vyrovil ich, ale prehodil PUT/POST metody + nevytvoril DELETE metody]
```

**Prečo to vzniklo:**
```
[Kedze som munezadefinoval crud operation, mohol cerpat data odhocikadial, mal som ho odkazat na konkretny zdroj, alebo mu vymenovat, co to presne musi obsahovat. To som sprvil pri Userovi, pri produktoch a orderoch som ho odkazal len na CRUD]
```

**Ako som to vyriešil:**
```
[Poukazal som mu, ze nevytvoril endpointy pre delete a zamenil POST s PUT metodou]
```

**Čo som sa naučil:**
```
[Od AI necakat, ze uhadne, co chceme spravit. Treba mu explicitne napisat, ak nieco konkretne vyzadujeme. Ale ak je nam to jedno, akym stylom to spravi, tak je to v poriadku, ale vysledok tomu zodpoveda]
```

---

### Problém #2: Neupratal si po sebe

**Čo sa stalo:**
```
Pri viacerych promptoch som vyzadoval, aby si AI nasledne spravilo kolecko a vyskusalo, ci vsetko spravne funguje a ci data sa upravia v db. Nenapisal som mu explicitne, aby po sebe upratal
```

**Prečo:**
```
Ocakaval som, ze to spravi automaticky, ze vrati projekt v spustitelnom stave.
```

**Riešenie:**
```
Treba mu napisat, aby po sebe zahodil testovacie scripty a vypol vsetky procesy, ktore pouzival pri vytvarani kodu a jeho kontrole
```

**Learning:**
```
Neocakavat, napisat, co chceme.
```

## 4. Kľúčové Poznatky

### 4.1 Čo fungovalo výborne

**1.** 
```
[vsetky prompty, kedze som si na nich dal zalezat po par, minimalnych dopytaniach fungovali spravne, celkovo som mal pocit, ze to nie je mozne, ze to funguje vsetko :D]
```

### 4.2 Čo bolo náročné

**1.** 
```
[Cele zadanie mi robilo problem, technologie (docker and postgresql + verzovanie db migracie) nerobievam s nimi v praci a tak som sa musel velmi spoliehat na AI nastroj, ze to urobi on, prijemne ma prekvapil ]
```

**2.** 
```
[nepacila sa mi struktura prjektu, ani nugety, ktore AI pouzil, ale na jeho obranu som mu ani ziadne z toho nedefinoval. Keby som mal viac casu, tak by sa s tym dalo viac pohrat]
```

**3.** 
```
[narocne mu dat nejaky code style na ktory som zvyknuty, lebo to bol cisty projekt, kde neboli ziadne priklady, od nicoho co by sa odrazil. Nemam ani nejake svoje referencne projekty, od ktorych by sa mohol odrazit.]
```

---

### 4.3 Best Practices ktoré som objavil

**1.** 
```
[Ak chcem pouzit konkretny nuget, treba mu ho vyspecifikovat]
```

**2.** 
```
[Ked ho ziadam o to, aby cely workflow otestoval, musim mu napisat, aby aj vsetko po sebe upratal a vypol procesy]
```

**3.** 
```
[Pri zadavani promptu nemozem byt lenivy, cim viac energie a prace do promptu vlozim, o to menej prace potom budem mat]
```

---

### 4.4 Moje Top 3 Tipy Pre Ostatných

**Tip #1:**
```
[nevyuzivat claude code na vsetko, velmi rychlo mina tokeny. Pouzivat ho na PR, commity, branche nie je moc efektivne vyuzitie nastroja, on sa pozrie na vsetky zmeny a spracovava ich, zerie to prilis vela zdrojov. ]
```

**Tip #2:**
```
ak idem vyuzit AI, kde mam obmedzene zdroje, pripravit poriadny prompt, agent spravy len taku dobru pracu, ake dobre instrukcie dostane
```

**Tip #3:**
```
nebat sa skusat spociatku experimentovat a zistovat hranice, resp. zmysel vyuzitia AI nastroja.
```

**Tip #4:**
```
AI si nedomysla co chceme, treba kontrolovat vystupy a korigovat ho este kym nespravi vsetky upravy. Potom to uz bude len narocnejsie.
```

---

## 6. Reflexia a Závery

### 6.1 Efektivita AI nástrojov

**Ktorý nástroj bol najužitočnejší?** [claude code]

**Prečo?**
```
vsestranny, mozem pouzivat z konzoly v hociakom prostredi. Cez to context engineering mi dava obrovsky zmysel, hned som zaviedol do praxe. Aj ine AI nastroje su toho schopne, neskusal som, ale claude code je urcite dobre riesenie.
```

**Ktorý nástroj bol najmenej užitočný?** asi Copilot v mode Ask

**Prečo?**
```
nikdy som preto nenasiel pouzitie, ak chcem mozem sa spytat bezneho LLM alebo mi vzdy stacil aj copilot v edit alebo Agent mode
```

---

### 6.2 Najväčšie prekvapenie
```
[Čo ťa najviac prekvapilo pri práci s AI?]
```
najviac ma prekvapilo, aky velky rozdiel robi, ked zadame kontext AI, vzdy som si myslel, ze Agent si cely ten kontext nejak domysli, resp. ze moje prompty su jednoznacne, ale neboli.
---

### 6.3 Najväčšia frustrácia
```
[Čo bolo najfrustrujúcejšie?]
```
ked som pouzil context engineering v praci a po vyse hodine mi vyplul slabe az zle riesenie, lebo som zle zadal prompt a povedal som si, ze to hadam vydedukuje :D
---

### 6.4 Najväčší "AHA!" moment
```
[Ked som rozsiril kontext a velkost promptu, uplne to zmenilo moj pohlad na to, co so spravnym zadanim vie AI spravit]
```

---

### 6.5 Čo by som urobil inak
```
[Keby si začínal znova, čo by si zmenil?]
```

### 6.6 Hlavný odkaz pre ostatných
```
[context engineering je urcite cesta, ktora zlepsi vyvoj, nie je to ten isty vysledok ako pisanie promptu do AI]
```
