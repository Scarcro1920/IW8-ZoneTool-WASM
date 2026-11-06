# IW8 ZoneTool WASM — état réel du projet

## Ce qui fonctionne
- Éditeur 3D navigateur Three.js : création de blocs, murs, rampes, spawns, lumières et terrains; sélection, édition, caméra, sauvegarde/import de projets.
- Export des entités et paquet ZIP de préparation géométrique.
- Module WASM compilé par GitHub Actions.
- Préflight WASM strict : vérifie les fichiers de base et les champs Replay indispensables (DPVS, probes, définitions, lighting, collision).
- Compilateur natif Windows x64 construit séparément avec MSVC; il s'agit du vrai compilateur ZoneTool.
- Lanceur PowerShell : sélection des dossiers, Replay et collision native MWCOLL; lance la compilation puis `validate-output`.
- Déploiement GitHub Pages activé automatiquement dans le workflow.

## Site
https://scarcro1920.github.io/IW8-ZoneTool-WASM/

## Limite actuelle — pas de faux .ff
L'export du navigateur est volontairement nommé paquet de préparation (`*_geometry_staging.zip`). Il contient une géométrie grise de travail et un fichier `*.render.geometry-draft.json`, pas un dump Replay compilable.

Un vrai dump Replay doit inclure notamment :
- render.json conforme, surfaces, topologie DPVS (planes/nodes/cells), probes de réflexion et image de probes;
- material.json / techset.json natifs, données de shader compilées et images référencées;
- lighting.json valide;
- collision Replay TAG0 déjà préparée, ou entrée MWCOLL valide avec le bon exécutable Replay.

Le projet ne génère pas encore automatiquement les données natives DPVS, probes, matériaux/techsets/shaders et textures à partir des simples primitives de l'éditeur. Un fichier Havok fourni est copié dans le paquet mais sa validité n'est pas certifiée par le navigateur.

## Procédure de compilation réelle
1. Préparer un dump complet conforme à `docs/INPUT_FORMAT.md` (inclus dans l'artifact du compilateur).
2. Télécharger `iw8-zonetool-windows-x64` depuis le workflow **Build native IW8 ZoneTool (Windows)**.
3. Placer `Launch-ZoneTool.ps1` à côté de `iw8-zonetool.exe`, lancer le script sous Windows, sélectionner le dump, la sortie, Replay et éventuellement MWCOLL.
4. Le script lance `build-map`, puis `validate-output`. La réussite de ces deux étapes est le critère de validation du fastfile.

Ne pas considérer le paquet de préparation du navigateur comme une map .ff jouable.
