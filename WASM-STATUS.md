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

## Export natif minimal généré par le navigateur
L'export produit maintenant `*_iw8_dump.zip` contenant :
- `render.json` avec matériau stock `$default`, surfaces triangulées, DPVS à une cellule et un arbre AABB couvrant les surfaces;
- un cubemap et une image array RGBA8 synthétiques pour le probe de réflexion;
- `lighting.json` avec direction et up normalisés/perpendiculaires;
- un fichier `MWCOLL12` généré avec un hull boîte englobante par surface exportée, pour que le baker Replay crée le Havok;
- les entités et un README.

Les hulls collision sont des boîtes englobantes : c'est une collision graybox approximative, pas une collision précise des triangles. Les probes sont neutres/synthétiques. Il faut compiler avec l'exécutable Replay exact, puis passer `validate-output`. La génération est conforme aux structures lues par le parser source, mais elle n'est pas déclarée jouable tant que le build natif réel n'a pas réussi sur le dump exporté.

Les matériaux stock `$default` sont utilisés pour éviter de fabriquer des shaders compilés. Les matériaux custom, techsets, lightmaps et shaders natifs ne sont pas générés.

## Procédure de compilation réelle
1. Préparer un dump complet conforme à `docs/INPUT_FORMAT.md` (inclus dans l'artifact du compilateur).
2. Télécharger `iw8-zonetool-windows-x64` depuis le workflow **Build native IW8 ZoneTool (Windows)**.
3. Placer `Launch-ZoneTool.ps1` à côté de `iw8-zonetool.exe`, lancer le script sous Windows, sélectionner le dump, la sortie, Replay et éventuellement MWCOLL.
4. Le script lance `build-map`, puis `validate-output`. La réussite de ces deux étapes est le critère de validation du fastfile.

Ne pas considérer le paquet de préparation du navigateur comme une map .ff jouable.
