# IW8 ZoneTool WASM — état du port

## Éditeur 3D ajouté
L'interface web inclut maintenant un éditeur de scène 3D Three.js :
- vue 3D orbitale, zoom, grille et éclairage;
- création de blocs, murs, rampes, points de spawn, lumières et terrain;
- sélection, déplacement par coordonnées, échelle, couleur et suppression;
- sauvegarde/import d'un projet `.ztmap.json`;
- export préliminaire d'un fichier d'entités.

Le projet `.ztmap.json` est le format de travail de l'éditeur. Il n'est pas encore un dump ZoneTool natif prêt à compiler.

## WASM actuel
Le module Emscripten compile `wasm/wasm_main.cpp` et vérifie la présence des fichiers d'entrée attendus pour une map. Il ne sérialise pas encore les assets IW8 et ne produit pas de fastfiles.

## Prochaine étape technique
Porter les sérialiseurs IW8 utilisés par `build-map`, convertir la géométrie de l'éditeur vers le schéma render IW8, générer des définitions material/techset valides, puis traiter la collision Havok Replay. La compilation .ff ne doit être annoncée qu'après validation dans Replay 1.20.
