#include <emscripten.h>
#include <cstdio>
#include <string>
#include <vector>
#include <filesystem>
#include <fstream>
#include <sstream>
namespace fs = std::filesystem;
static std::string g_report;

static bool is_file(const fs::path& p) {
    std::error_code ec;
    return fs::is_regular_file(p, ec) && !ec;
}
static std::string read_text(const fs::path& p) {
    std::ifstream f(p, std::ios::binary);
    if (!f) return {};
    std::ostringstream s;
    s << f.rdbuf();
    return s.str();
}
static bool has_key(const std::string& json, const std::string& key) {
    return json.find("\"" + key + "\"") != std::string::npos;
}
extern "C" {
EMSCRIPTEN_KEEPALIVE const char* ztw_version() {
    return "IW8 ZoneTool WASM 0.4 (Replay graybox dump preflight)";
}
EMSCRIPTEN_KEEPALIVE int ztw_check_dump(const char* root, const char* mapName) {
    g_report.clear();
    if (!root || !mapName || !*mapName) {
        g_report = "ERROR: dump path or map name is empty.";
        return 0;
    }
    const std::string map(mapName);
    if (map.rfind("mp_", 0) != 0 || map.size() > 15) {
        g_report = "ERROR: map id must start with mp_ and contain at most 15 characters.";
        return 0;
    }
    const fs::path dir(root);
    const fs::path mp = dir / "maps" / "mp";
    const std::string asset = map + ".d3dbsp";
    const fs::path ents = dir / (map + "_iw8_ents.txt");
    const fs::path renderPath = mp / (asset + ".render.json");
    const fs::path lightingPath = mp / (asset + ".lighting.json");
    const fs::path havokPath = mp / (asset + ".havok");
    const fs::path mwcollPath = mp / (asset + ".mwcoll");

    bool ok = true;
    g_report = "IW8 ZoneTool — vérification stricte du dump\n";
    const auto check = [&](const fs::path& p, const char* label) {
        const bool found = is_file(p);
        g_report += (found ? "[OK] " : "[MANQUANT] ");
        g_report += label;
        g_report += "\n";
        if (!found) ok = false;
    };
    check(ents, "entités IW8");
    check(renderPath, "render.json");
    check(lightingPath, "lighting.json");
    const bool hasHavok = is_file(havokPath);
    const bool hasCollision = hasHavok || is_file(mwcollPath);
    g_report += (hasCollision ? "[OK] " : "[OPTIONNEL] ");
    g_report += hasHavok ? "collision .havok présente\\n" :
                is_file(mwcollPath) ? "entrée MWCOLL présente (bake Replay requis)\\n" :
                "collision absente : ajouter MWCOLL ou Havok avant build-map\\n";

    const std::string render = read_text(renderPath);
    if (!render.empty()) {
        const std::vector<std::string> keys = {
            "material", "surfaces", "dpvs", "reflectionProbes",
            "reflectionProbeArrayImage"
        };
        for (const auto& key : keys) {
            const bool found = has_key(render, key);
            g_report += (found ? "[OK] " : "[MANQUANT] ");
            g_report += "render.";
            g_report += key;
            g_report += "\n";
            if (!found) ok = false;
        }
    }
    const std::string lighting = read_text(lightingPath);
    if (!lighting.empty()) {
        for (const auto& key : {std::string("schema"), std::string("intensity"),
                                std::string("color"), std::string("direction"),
                                std::string("up")}) {
            const bool found = has_key(lighting, key);
            g_report += (found ? "[OK] " : "[MANQUANT] ");
            g_report += "lighting.";
            g_report += key;
            g_report += "\n";
            if (!found) ok = false;
        }
    }
    g_report += "\n";
    if (ok) {
        g_report += "Structure et champs requis présents. La compilation native et validate-output restent obligatoires : ce contrôle ne certifie pas les données binaires, les références de shaders, les textures ni la validité du Havok.\n";
        return 1;
    }
    g_report += "DUMP INCOMPLET : corrige les champs ou fichiers signalés avant la compilation native.\n";
    return 0;
}
EMSCRIPTEN_KEEPALIVE const char* ztw_last_report() { return g_report.c_str(); }
}
