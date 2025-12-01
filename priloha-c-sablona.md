# AI Workflow Dokumentácia

**Meno:** 

**Dátum začiatku:** 

**Dátum dokončenia:** 

**Zadanie:** Frontend / Backend

---

## 1. Použité AI Nástroje

Vyplň približný čas strávený s každým nástrojom:

- [ ] **Cursor IDE:** _____ hodín
- [ ] **Claude Code:** 2 hodín  
- [ ] **GitHub Copilot:** _____ hodín
- [ ] **ChatGPT:** _____ hodín
- [ ] **Claude.ai:** _____ hodín
- [ ] **Iné:** 

**Celkový čas vývoja (priližne):** _____ hodín

---

## 2. Zbierka Promptov

> 💡 **Tip:** Kopíruj presný text promptu! Priebežne dopĺňaj po každej feature.

### Prompt #1: _________________________________
## FEATURE:

**Nástroj:** [ Claude Code]  
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
```



### Prompt #2: _________________________________

**Nástroj:** claude code  
**Kontext:** po dokonceni mojho prp som potreboval este upravit nazov stlpca a skontrolovat ci vsetko funguje

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
