# Gauniv - Plateforme de Distribution de Jeux

Une plateforme complète de distribution de jeux vidéo avec interface web d'administration et client MAUI cross-platform (MacOS et windows ).

## Installation

1. **Cloner le projet**


2. **Démarrer le Serveur Web**
```
dotnet run --project Gauniv.WebServer/Gauniv.WebServer.csproj --urls "http://0.0.0.0:5231"
```

Le serveur démarre sur **http://localhost:5231**

```Le serveur DOIT tourner avant de lancer le client MAUI```

3. **Lancer l'interface Web**

Ouvrez votre navigateur : **http://localhost:5231**

4. **Lancer le client MAUI**

**Sur macOS :**
```bash
dotnet build Gauniv.Client/Gauniv.Client.csproj -f net10.0-maccatalyst -c Debug
Open ./Gauniv.Client/bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/Gauniv.app
```

**Sur Windows :**
```bash
dotnet build Gauniv.Client/Gauniv.Client.csproj -f net10.0-windows -c Debug
```


### Compte Administrateur
- **Username** : `admin`
- **Email** : `admin@gauniv.com`
- **Mot de passe** : `admin`
- **Accès** : Interface web admin + Client MAUI

### Compte Utilisateur
- **Username** : `user`
- **Email** : `user@gauniv.com`
- **Mot de passe** : `user`
- **Accès** : Interface Web (mais peu d'utilité) + de fonctionnalités sur le Client MAUI 

### Créer un Nouveau Compte

**Via le Client MAUI :**
1. Ouvrir l'application MAUI
2. Aller sur l'onglet **Profil**
3. Cliquer sur **"Pas encore de compte ? S'inscrire"**
4. Remplir le formulaire avec :
   - Email 
   - Mot de passe
   - Prénom et Nom
5. Le username sera automatiquement extrait de l'email (tout ce qui est avant le @)

**Connexion :**
- Vous pouvez vous connecter avec votre **email** OU votre **username**
- Exemple : `john` ou `john@live.fr` fonctionnent tous les deux

## 🗄️ Accès à la Base de Données

### Localisation

La base de données SQLite se trouve dans :
```
Gauniv.WebServer/Database/gauniv.db
```

### Tables Principales de la DB SQLite

- **`User`** : Comptes utilisateurs (Id, UserName, Email, FirstName, LastName, IsAdmin...)
- **`Games`** : Jeux disponibles (Id, Title, Description, Price, Categories, ImageUrl, Payload)
- **`Categories`** : Catégories de jeux
- **`UserFriends`** : Relations d'amitié (SourceUserId, TargetUserId, IsAccepted)

### Requêtes Utiles

**Lister tous les utilisateurs :**
```sql
SELECT Id, UserName, Email, FirstName, LastName, IsAdmin FROM User;
```

**Voir tous les jeux :**
```sql
SELECT Id, Title, Price, Categories FROM Games;
```

**Voir les relations "amis" :**
```sql
SELECT 
    u1.UserName as Source, 
    u2.UserName as Target, 
    uf.IsAccepted 
FROM UserFriends uf
JOIN User u1 ON uf.SourceUserId = u1.Id
JOIN User u2 ON uf.TargetUserId = u2.Id;
```

## Fonctionnalités

### Interface Web (Admin)

**Actions Administrateur :**
-  Ajouter/Modifier/Supprimer des jeux
-  Gérer les catégories
-  Upload d'images pour les jeux
-  Consulter tous les jeux et utilisateurs

**URL** : http://localhost:5231/Admin

### Client MAUI:

**Fonctionnalités :**
-  **Catalogue** : Parcourir tous les jeux avec filtres (prix, catégorie, possédé)
-  **Détails** : Voir description, prix, catégories
-  **Achat** : Acheter des jeux
-  **Bibliothèque** : Gérer vos jeux achetés
-  **Téléchargement** : Télécharger et lancer les jeux
-  **Amis** : Ajouter/Accepter/Voir amis avec statut en ligne
-  **Profil** : Modifier prénom, nom, email
-  **Admin** : Accès admin pour gérer les jeux 

### API REST

**Endpoints :**
- `POST /Bearer/login` : Connexion (email ou username)
- `POST /Bearer/register` : Inscription
- `GET /api/Games` : Liste des jeux (avec pagination et filtres)
- `GET /api/Categories` : Liste des catégories
- `POST /api/Games/{id}/purchase` : Acheter un jeu
- `GET /api/Games/owned` : Jeux possédés
- `GET /api/Friends` : Liste d'amis
- `POST /api/Friends/request` : Demande d'ami

**Documentation API** : http://localhost:5231/openapi/v1.json

## Sécurité

- **Authentification** : ASP.NET Identity avec tokens Bearer
- **Autorisation** : Rôles Admin/User
- **Connexion temps réel** : SignalR avec authentification


## Contributeurs

- **Farid Zennani**
- **Mouna Aarour**
