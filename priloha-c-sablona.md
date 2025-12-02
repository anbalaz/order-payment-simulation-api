# AI Workflow Dokumentácia

**Meno:** 

**Dátum začiatku:** 

**Dátum dokončenia:** 

**Zadanie:** Frontend / Backend

---

## 1. Použité AI Nástroje

Vyplň približný čas strávený s každým nástrojom:

- [ ] **Cursor IDE:** _____ hodín
- [ ] **Claude Code:** 5 hodín
- [ ] **GitHub Copilot:** _____ hodín
- [ ] **ChatGPT:** _____ hodín
- [ ] **Claude.ai:** _____ hodín
- [ ] **Iné:** 

**Celkový čas vývoja (priližne):** _____ hodín

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




















---

## 3. Problémy a Riešenia 

> 💡 **Tip:** Problémy sú cenné! Ukazujú ako riešiš problémy s AI.

### Problém #1: _________________________________

**Čo sa stalo:**
```
[Detailný popis problému - čo nefungovalo? Aká bola chyba?]
```

**Prečo to vzniklo:**
```
[Tvoja analýza - prečo AI toto vygeneroval? Čo bolo v prompte zlé?]
```

**Ako som to vyriešil:**
```
[Krok za krokom - čo si urobil? Upravil prompt? Prepísal kód? Použil iný nástroj?]
```

**Čo som sa naučil:**
```
[Konkrétny learning pre budúcnosť - čo budeš robiť inak?]
```

**Screenshot / Kód:** [ ] Priložený

---

### Problém #2: _________________________________

**Čo sa stalo:**
```
```

**Prečo:**
```
```

**Riešenie:**
```
```

**Learning:**
```
```

## 4. Kľúčové Poznatky

### 4.1 Čo fungovalo výborne

**1.** 
```
[Príklad: Claude Code pre OAuth - fungoval first try, zero problémov]
```

**2.** 
```
```

**3.** 
```
```

**[ Pridaj viac ak chceš ]**

---

### 4.2 Čo bolo náročné

**1.** 
```
[Príklad: Figma MCP spacing - často o 4-8px vedľa, musel som manuálne opravovať]
```

**2.** 
```
```

**3.** 
```
```

---

### 4.3 Best Practices ktoré som objavil

**1.** 
```
[Príklad: Vždy špecifikuj verziu knižnice v prompte - "NextAuth.js v5"]
```

**2.** 
```
```

**3.** 
```
```

**4.** 
```
```

**5.** 
```
```

---

### 4.4 Moje Top 3 Tipy Pre Ostatných

**Tip #1:**
```
[Konkrétny, actionable tip]
```

**Tip #2:**
```
```

**Tip #3:**
```
```

---

## 6. Reflexia a Závery

### 6.1 Efektivita AI nástrojov

**Ktorý nástroj bol najužitočnejší?** _________________________________

**Prečo?**
```
```

**Ktorý nástroj bol najmenej užitočný?** _________________________________

**Prečo?**
```
```

---

### 6.2 Najväčšie prekvapenie
```
[Čo ťa najviac prekvapilo pri práci s AI?]
```

---

### 6.3 Najväčšia frustrácia
```
[Čo bolo najfrustrujúcejšie?]
```

---

### 6.4 Najväčší "AHA!" moment
```
[Kedy ti došlo niečo dôležité o AI alebo o developmente?]
```

---

### 6.5 Čo by som urobil inak
```
[Keby si začínal znova, čo by si zmenil?]
```

### 6.6 Hlavný odkaz pre ostatných
```
[Keby si mal povedať jednu vec kolegom o AI development, čo by to bylo?]
```
