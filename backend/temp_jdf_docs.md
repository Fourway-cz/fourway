# JDF Soubor

PS.: Data jsou v **Windows-1250 (`CP1250`),** při otevření v UTF-8  se poškodí diakritika.

## Jak jsou data uchovávána

#### Syntaxe

- Položky jsou odděleny čárkou (`,`), každá hodnota je v uvozovkách (`"..."`). Prázdná hodnota je zapsána jako `""`.
- Každý řádek je zakončený středníkem.

#### Formáty hodnot

- **Časové údaje:** 4 číslice ve formátu `hhmm` (např. `0455` = 04:55).
- **Datumové údaje:** 8 číslic ve formátu `ddmmyyyy` (např. `02112026` = 2. 11. 2026).
- **Číslo linky:** 6 číslic (např. `691455`, u mezinárodních `000...`).
- **Symboly v časech spojů:**
    - `<` Spoj jede jinou trasou / kolem této zastávky vůbec neprojíždí.
    - `|` Spoj zastávkou pouze projíždí bez zastavení.
    - `""` Čas není vyplněn (typicky příjezd na výchozí zastávce nebo odjezd na konečné).

### Relační architektura (provázání souborů)

- **Linka:** Identifikována dvojicí `(CisloLinky, RozliseniLinky)`.
- **Dopravce:** Identifikován `(ICDopravce, CisloDopravce)`.
- **Spoj:** Identifikován `(CisloLinky, RozliseniLinky, CisloSpoje)`.
- **Trasa linky (Zaslinky):** Zastávky seřazené podle pořadového `TarifniCislo` (1 až $N$).
- **Jízdní řád spoje (Zasspoje):** Propojuje konkrétní spoj s tarifním bodem na trase: `(CisloLinky, CisloSpoje, TarifniCislo)`.

## Co za informace jednotlivé soubory obsahují

- V zipech se nachází 15 typů souborů

// total ai ale přepisovat to nebudu

| Soubor | Povinnost | Hlavní účel |
| --- | --- | --- |
| **`VerzeJDF.txt`** | Povinný | Deklarace verze JDF (`1.8`, `1.9`, `1.10`, `1.11`) a datum exportu. |
| **`Dopravci.txt`** | Povinný | Seznam dopravců: IČO, název firmy, sídlo, telefon, e-mail, web a číslo provozovny. |
| **`Linky.txt`** | Povinný | Kmenové údaje linky: číslo, název trasy, druh dopravy (MHD/kraj), platnost JŘ od–do. |
| **`Zastavky.txt`** | Povinný | Číselník zastávek: obec, část obce, bližší určení, okres (SPZ), stát a vybavenost. |
| **`Zaslinky.txt`** | Povinný | Trasa linky: posloupnost zastávek (`TarifniCislo`), zóny IDS a tarifní podmínky. |
| **`Spoje.txt`** | Povinný | Seznam všech spojů (jízd) linky a jejich základní pevné kódy (pracovní dny, bezbariérovost). |
| **`Zasspoje.txt`** | Povinný | Hlavní matice JŘ: časy příjezdů a odjezdů, ujeté kilometry a značky projíždění (`<`, `\|`). |
| **`Pevnykod.txt`** | Povinný | Číselník značek: vazba kódu na symbol v tiskovém JŘ (`X`, `+`, `@`, `~`, `R`, `(`). |
| **`Caskody.txt`** | Povinný | Kalendářní omezení: přesná data, prázdniny, liché/sudé týdny, kdy spoj jede či nejede. |
| **`Udaje.txt`** | Volitelný | Všeobecné textové poznámky pod jízdním řádem (tarify, SPP, kontakty na dispečink). |
| **`LinExt.txt`** | Volitelný *(od 1.10)* | Rozšíření pro IDS a MHD: zkrácené číslo linky (např. `455`, `108`) a kód krajského IDS. |
| **`Navaznosti.txt`** | Volitelný *(od 1.11)* | Garantované přestupy a maximální čekací doby spojů (v minutách) na jiné spoje/vlaky. |
| **`Altdop.txt`** | Volitelný *(od 1.11)* | Evidence alternativních dopravců (subdodavatelů) zajišťujících pouze vybrané spoje. |
| **`Altlinky.txt`** | Volitelný *(od 1.11)* | Párování mezinárodní linky na licenční číslo v zahraničí (např. na Slovensku). |
| **`Mistenky.txt`** | Volitelný *(od 1.11)* | Pokyny a prodejní kanály pro nákup místenek a předprodej jízdenek (AMSBus apod.). |

### Detailní popis a obsahy souborů

#### `VerzeJDF.txt` (Verze specifikace)

> *Ukázka řádku:* `"1.11","","","","02112026","";`
> 

| # | Název položky | Datový typ | Popis / Povolené hodnoty |
| --- | --- | --- | --- |
| **1** | `CisloVerze` | Text | Verze specifikace (`"1.8"`, `"1.9"`, `"1.10"`, `"1.11"`). |
| **2** | `IdentifikaceDavky` | Text | Číslo / kód exportní dávky (volitelné). |
| **3** | `PopisDavky` | Text | Doplňkový popis dávky (volitelné). |
| **4** | `OznaceniZpracovatele` | Text | Identifikátor software nebo organizace zpracovatele. |
| **5** | `DatumExportu` | Datum (`ddmmyyyy`) | Datum vygenerování datové dávky (např. `"02112026"`). |
| **6** | `Rezerva` | Text | Rezervní pole. |

#### `Dopravci.txt` (Číselník dopravců a provozoven)

> *Ukázka řádku:* `"15040500","","Transdev Čechy s.r.o.","1","","Soukenická 242, Vrchlabí","","","+420 775 881 765","","trutnovsko@transdev.com","www.audisbus.cz","1";`
> 

| # | Název položky | Datový typ | Popis / Povolené hodnoty |
| --- | --- | --- | --- |
| **1** | `ICDopravce` | Text (8) | IČO dopravce (např. `"15040500"`). |
| **2** | `DICDopravce` | Text | DIČ dopravce (často prázdné). |
| **3** | `NazevDopravce` | Text | Obchodní jméno dopravce dle obchodního rejstříku. |
| **4** | `DruhDopravce` | Kód | Právní forma (`1` = právnická osoba, `2` = fyzická osoba). |
| **5** | `JmenoFyzickeOsoby` | Text | Jméno a příjmení podnikatele (u FO). |
| **6** | `SidloProvozovna` | Text | Adresa sídla nebo název konkrétní provozovny. |
| **7** | `TelefonPredvolba` | Text | Telefonní předvolba. |
| **8** | `Telefon1` | Text | Telefonní kontakt (kancelář / dispečink). |
| **9** | `Telefon2` | Text | Kontaktní telefon pro cestující / infolinka. |
| **10** | `Fax` | Text | Faxové číslo (historické pole). |
| **11** | `Email` | Text | Kontaktní e-mailová adresa. |
| **12** | `Web` | Text | Webové stránky dopravce. |
| **13** | `CisloDopravce` | Celé číslo | Pořadové číslo provozovny dopravce na lince (`1`, `2`…). Klíč pro vazbu! |

#### `Linky.txt` (Kmenová data linky)

> *Ukázka řádku:* `"691455","455 Vrchlabí-Dolní Kalná-Zadní Ždírnice","15040500","V","A","0","0","0","0","","","","","14062026","12122026","1","1";`
> 

| # | Název položky | Datový typ | Popis / Povolené hodnoty |
| --- | --- | --- | --- |
| **1** | `CisloLinky` | Text (6) | Celostátní licenční číslo linky v CIS JŘ (např. `"691455"`). |
| **2** | `NazevLinky` | Text | Textový popis trasy linky (*Odkud – Kam*). |
| **3** | `ICDopravce` | Text (8) | IČO hlavního licencovaného dopravce (vazba do `Dopravci.txt`). |
| **4** | `TypLinky` | Kód (1) | Druh linky: `V` (vnitrokrajská), `A` (MHD), `D` (dálková), `M`/`Z` (mezinárodní), `P` (příměstská). |
| **5** | `DopravniProstredek` | Kód (1) | Typ vozidla: `A` (autobus), `T` (trolejbus), `E` (tramvaj), `V` (vlak). |
| **6** | `JednosmernyProvoz` | Kód (1) | `0` = obousměrná linka, `1` = jednosměrná / okružní linka. |
| **7–9** | `Rezerva1_3` | Text | Rezervní příznaky (`0`, `0`, `0`). |
| **10** | `PlatnostLicenceOd` | Datum (`ddmmyyyy`) | Počátek platnosti licence. |
| **11** | `PlatnostLicenceDo` | Datum (`ddmmyyyy`) | Konec platnosti licence. |
| **12** | `CisloLicence` | Text | Číslo licence / správního rozhodnutí. |
| **13** | `SchvalujiciOrgan` | Text | Schvalující úřad (např. Krajský úřad). |
| **14** | `PlatnostOd` | Datum (`ddmmyyyy`) | Začátek platnosti jízdního řádu (např. `"14062026"`). |
| **15** | `PlatnostDo` | Datum (`ddmmyyyy`) | Konec platnosti jízdního řádu (např. `"12122026"`). |
| **16** | `CisloDopravce` | Celé číslo | Odkaz na provozovnu v `Dopravci.txt` (sloupec 13). |
| **17** | `RozliseniLinky` | Celé číslo | Rozlišení varianty linky (standardně `"1"`). |

#### `Zastavky.txt` (Číselník fyzických zastávek)

> *Ukázka řádku:* `"4","Dolní Kalná","","aut.st.","TU","CZ","","","","","","";`
> 

| # | Název položky | Datový typ | Popis / Povolené hodnoty |
| --- | --- | --- | --- |
| **1** | `CisloZastavky` | Celé číslo | Interní ID zastávky v rámci dané dávky (`1`, `2`…). |
| **2** | `NazevObce` | Text | Název obce (např. `"Dolní Kalná"`). |
| **3** | `CastObce` | Text | Část obce / místní část (např. `"Starý Pelhřimov"`, volitelné). |
| **4** | `BlizsiUrceni` | Text | Bližší lokalita (např. `"aut.st."`, `"škola"`, `"žel.st."`). |
| **5** | `Okres` | Text (2) | Kód okresu – bývalá SPZ (např. `"TU"`, `"PE"`, `"JC"`). |
| **6** | `Stat` | Text (2) | Kód státu (např. `"CZ"`, `"SK"`, `"PL"`). |
| **7–11** | `PevnyKod1_5` | Kód | Pevné kódy vybavenosti zastávky (odkaz na `Pevnykod.txt`). |
| **12** | `Rezerva` | Text | Rezervní pole. |

#### `Zaslinky.txt` (Trasa linky a tarifní body)

> *Ukázka řádku:* `"691455","1","123","16","","","","","1";`
> 

| # | Název položky | Datový typ | Popis / Povolené hodnoty |
| --- | --- | --- | --- |
| **1** | `CisloLinky` | Text (6) | Licenční číslo linky. |
| **2** | `TarifniCislo` | Celé číslo | Pořadové číslo zastávky na trase linky ($1 \dots N$). Klíč řazení trasy! |
| **3** | `TarifniPasmo` | Text | Zóna nebo tarifní pásmo IDS (např. `"123"` nebo `"121,122"`). |
| **4** | `CisloZastavky` | Celé číslo | Odkaz do číselníku `Zastavky.txt` (sloupec 1). |
| **5–8** | `PevnyKod1_4` | Kód | Pevné kódy odbavení na lince (např. `18` = zastávka na znamení). |
| **9** | `RozliseniLinky` | Celé číslo | Rozlišení linky (`"1"`). |

#### `Spoje.txt` (Seznam spojů linky)

> *Ukázka řádku:* `"691455","1","1","14","","","","","","","","","","1";`
> 

| # | Název položky | Datový typ | Popis / Povolené hodnoty |
| --- | --- | --- | --- |
| **1** | `CisloLinky` | Text (6) | Licenční číslo linky. |
| **2** | `CisloSpoje` | Celé číslo | Číslo spoje (lichá = tam, sudá = zpět). |
| **3–12** | `PevnyKod1_10` | Kód | Až 10 pevných kódů spoje (`1`=prac. dny, `14`=vozíčkář, `21`=kola). |
| **13** | `Rezerva` | Text | Rezervní pole. |
| **14** | `RozliseniLinky` | Celé číslo | Rozlišení linky (`"1"`). |

#### `Zasspoje.txt` (Jízdní řád: Časy a kilometráž spojů)

> *Ukázka řádku:* `"691455","1","2","15","","","","","0","2","0537","0537","0537","0537","1";`
> 

| # | Název položky | Datový typ | Popis / Povolené hodnoty |
| --- | --- | --- | --- |
| **1** | `CisloLinky` | Text (6) | Licenční číslo linky. |
| **2** | `CisloSpoje` | Celé číslo | Číslo spoje. |
| **3** | `TarifniCislo` | Celé číslo | Pořadí zastávky na trase (vazba na `Zaslinky.txt`). |
| **4** | `CisloZastavky` | Celé číslo | Identifikátor zastávky (vazba na `Zastavky.txt`). |
| **5** | `CisloStanoviste` | Text | Označení označníku / stanoviště (např. `"3"`). |
| **6–8** | `PevnyKodSpoje1_3` | Kód | Kódy pro spoj na zastávce (např. `31` = nástup předem, `18` = na znamení). |
| **9** | `TarifniKm` | Celé číslo | Tarifní kilometry (Tkm) od výchozí zastávky. |
| **10** | `SkutecneKm` | Celé číslo | Skutečné kilometry (Skm) od výchozí zastávky. |
| **11** | `CasPrijezduMin` | Čas / Znak | Čas příjezdu min (`hhmm`, `<` = jede jinudy, `\|` = projíždí). |
| **12** | `CasOdjezduMin` | Čas / Znak | Čas odjezdu min (`hhmm`, `<`, `\|`). |
| **13** | `CasPrijezduMax` | Čas / Znak | Čas příjezdu max (v JDF 1.11 pro toleranci, jinak = sl. 11). |
| **14** | `CasOdjezduMax` | Čas / Znak | Čas odjezdu max (v JDF 1.11 pro toleranci, jinak = sl. 12). |
| **15** | `RozliseniLinky` | Celé číslo | Rozlišení linky (`"1"`). |

#### `Pevnykod.txt` (Číselník grafických značek)

> *Ukázka řádku:* `"1","X","";`
> 

| # | Název položky | Datový typ | Popis / Povolené hodnoty |
| --- | --- | --- | --- |
| **1** | `CisloPevnehoKodu` | Celé číslo | Číselný identifikátor kódu (např. `1`, `2`, `14`, `18`). |
| **2** | `ZnakSymbolu` | Znak / Text | Symbol v tiskovém JŘ (`X`=prac. dny, `+`=neděle, `@`=vozíčkář, `x`=znamení). |
| **3** | `PopisSymbolu` | Text | Textový popis významu (často prázdné, plyne z legislativy). |

#### `Caskody.txt` (Kalendářní omezení a provozní dny)

> *Ukázka řádku:* `"691455","1","1","42","4","22122025","02012026","","1";`
> 

| # | Název položky | Datový typ | Popis / Povolené hodnoty |
| --- | --- | --- | --- |
| **1** | `CisloLinky` | Text (6) | Licenční číslo linky. |
| **2** | `CisloSpoje` | Celé číslo | Číslo spoje, pro který omezení platí. |
| **3** | `IndexCaskodu` | Celé číslo | Pořadové číslo časového kódu u daného spoje (`1`, `2`…). |
| **4** | `OznaceniCaskodu` | Text | Označení poznámky v jízdním řádu (např. `"42"`, `"10"`, `"p"`). |
| **5** | `TypCaskodu` | Kód (1) | `1`=jede od-do, `2`=jede dne, `3`=nejede dne, `4`=nejede od-do, `5`/`6`=týdny. |
| **6** | `DatumOd` | Datum (`ddmmyyyy`) | Počátek intervalu platnosti omezení. |
| **7** | `DatumDo` | Datum (`ddmmyyyy`) | Konec intervalu platnosti omezení. |
| **8** | `TextPoznamky` | Text | Slovní popis nebo návaznost (např. spoj pokračuje na jiné lince). |
| **9** | `RozliseniLinky` | Celé číslo | Rozlišení linky (`"1"`). |

#### `Udaje.txt` (Textové poznámky pod jízdním řádem)

> *Ukázka řádku:* `"691455","1","Na lince platí tarif a SPP IREDO. Informace na www.dopravakhk.cz.","1";`
> 

| # | Název položky | Datový typ | Popis / Povolené hodnoty |
| --- | --- | --- | --- |
| **1** | `CisloLinky` | Text (6) | Licenční číslo linky. |
| **2** | `PoradoveCislo` | Celé číslo | Pořadové číslo řádku textu (`1`, `2`, `3`…). |
| **3** | `TextUdaje` | Text | Textové sdělení. Vlnovka `~` na začátku značí pokračování řádku. |
| **4** | `RozliseniLinky` | Celé číslo | Rozlišení linky (`"1"`). |

#### `LinExt.txt` (Rozšíření pro IDS a městskou dopravu)

> *Ukázka řádku:* `"691455","1","30522","455","1","","1";`
> 

| # | Název položky | Datový typ | Popis / Povolené hodnoty |
| --- | --- | --- | --- |
| **1** | `CisloLinky` | Text (6) | Celostátní licenční číslo linky (např. `"691455"`). |
| **2** | `PoradoveCislo` | Celé číslo | Pořadové číslo záznamu (`"1"`). |
| **3** | `KodDopravy` | Text (5) | Kód IDS (např. `30522` = IREDO, `30001` = PID, `30314` = IDPK). |
| **4** | `OznaceniLinky` | Text | Veřejné číslo linky pro cestující (např. `"455"`, `"108"`). |
| **5** | `PriznakIntegrace` | Kód (1) | `1` = plně integrována v IDS, `0` = částečně. |
| **6** | `Rezerva` | Text | Rezervní pole. |
| **7** | `RozliseniLinky` | Celé číslo | Rozlišení linky (`"1"`). |

#### `Navaznosti.txt` (Garantované přestupy)

> *Ukázka řádku:* `"m","691455","7","2","","","","","","","spoj 7 vyčká v zastávce Vrchlabí,aut.st. příjezdu spoje 1013 nejvýše 5 minut","1";`
> 

| # | Název položky | Datový typ | Popis / Povolené hodnoty |
| --- | --- | --- | --- |
| **1** | `TypNavaznosti` | Kód (1) | Typ vazby: `m` (malá návaznost), `M` (garantovaný přestup s hlášením). |
| **2** | `CisloLinky` | Text (6) | Číslo čekající linky. |
| **3** | `CisloSpoje` | Celé číslo | Číslo čekajícího spoje. |
| **4** | `TarifniCislo` | Celé číslo | Tarifní číslo zastávky, kde spoj vyčkává. |
| **5–10** | `ParametryVazby` | Text | Identifikace přípojné linky/spoje a čekací parametry. |
| **11** | `TextNavaznosti` | Text | Textové znění informace pro cestující včetně maximální doby čekání. |
| **12** | `RozliseniLinky` | Celé číslo | Rozlišení linky (`"1"`). |

#### `Altdop.txt` (Alternativní dopravci spojů)

> *Ukázka řádku:* `"691455","3","15040500","","","","","","","","","14062026","12122026","2","1";`
> 

| # | Název položky | Datový typ | Popis / Povolené hodnoty |
| --- | --- | --- | --- |
| **1** | `CisloLinky` | Text (6) | Licenční číslo linky. |
| **2** | `CisloSpoje` | Celé číslo | Číslo spoje, který alternativní dopravce provozuje. |
| **3** | `ICDopravce` | Text (8) | IČO alternativního dopravce. |
| **4–11** | `DoplnkoveUdaje` | Text | Doplňkové licenční údaje. |
| **12** | `PlatnostOd` | Datum (`ddmmyyyy`) | Začátek platnosti subdodávky. |
| **13** | `PlatnostDo` | Datum (`ddmmyyyy`) | Konec platnosti subdodávky. |
| **14** | `CisloDopravce` | Celé číslo | Odkaz na provozovnu v `Dopravci.txt` (sloupec 13). |
| **15** | `RozliseniLinky` | Celé číslo | Rozlišení linky (`"1"`). |

#### `Altlinky.txt` (Alternativní čísla mezinárodních linek)

> *Ukázka řádku:* `"000246","808901","SK","1";`
> 

| # | Název položky | Datový typ | Popis / Povolené hodnoty |
| --- | --- | --- | --- |
| **1** | `CisloLinky` | Text (6) | České licenční číslo mezinárodní linky (např. `"000246"`). |
| **2** | `AlternativniCislo` | Text | Licenční číslo linky v zahraničním registru (např. `"808901"`). |
| **3** | `Zeme` | Text (2) | Kód zahraničního státu (např. `"SK"`). |
| **4** | `RozliseniLinky` | Celé číslo | Rozlišení linky (`"1"`). |

#### `Mistenky.txt` (Rezervace míst a předprodej)

> *Ukázka řádku:* `"721380","0","ve všech předprodejních kancelářích AMS a na www.e-jizdenka.cz","1";`
> 

| # | Název položky | Datový typ | Popis / Povolené hodnoty |
| --- | --- | --- | --- |
| **1** | `CisloLinky` | Text (6) | Licenční číslo linky. |
| **2** | `CisloSpoje` | Celé číslo | Číslo spoje (`0` = informace platí paušálně pro celou linku). |
| **3** | `InformaceOMistenkach` | Text | Textové pokyny pro nákup místenek a adresy prodejních webů. |
| **4** | `RozliseniLinky` | Celé číslo | Rozlišení linky (`"1"`). |

### Danovo cheatsheet

- **Typ linky (`TypLinky`)**
    - `A` (MHD autobus)
    - `V` (vnitrokrajská)
    - `D` (dálková)
    - `M`/`Z` (mezinárodní)
    - `N` (zvláštní/neveřejná)
    - `P` (příměstská)
- **Dopravní prostředek (`DopravniProstredek`)**
    - `A` (autobus)
    - `T` (trolejbus)
    - `E` (tramvaj)
    - `V` (vlak)
    - `L` (lanovka)
    - `P` (přívoz)
- **Nejběžnější pevné kódy (`Pevnykod.txt`)**
    - `1` = jede v pracovní dny (`X`)
    - `2` = neděle a svátky (`+`)
    - `14` = bezbariérové vozidlo (`@`)
    - `18` = zastávka na znamení (`x`)
    - `21` = cyklobus (`(`)
    - `30` = vyloučena místní přeprava (`§`)