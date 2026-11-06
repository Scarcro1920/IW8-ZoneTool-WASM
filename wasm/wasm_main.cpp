#include <emscripten.h>
#include <cstdio>
#include <cstring>
#include <string>
#include <vector>
#include <filesystem>
#include <fstream>

namespace fs = std::filesystem;

static std::string readText(const fs::path& p) {
    std::ifstream f(p, std::ios::binary);
    if (!f) return {};
    return std::string((std::istreambuf_iterator<char>(f)), {});
}

extern "C" {
EMSCRIPTEN_KEEPALIVE
const char* ztw_version() {
    return "IW8 ZoneTool WASM bootstrap 0.1 (dump preflight only)";
}

EMSCRIPTEN_KEEPALIVE
int ztw_check_dump(const char* root, const char* mapName) {
    static std::string report;
    report.clear();
    if (!root || !mapName || !*mapName) {
        report = "ERROR: dump path or map name is empty.";
        return 0;
    }
    const fs::path dir(root);
    const std::string base = std::string(mapName) + ".d3dbsp";
    const std::vector<std::string> required = {
        std::string(mapName) + "_iw8_ents.txt",
        "maps/mp/" + base + ".render.json",
        "maps/mp/" + base + ".material.json",
        "maps/mp/" + base + ".techset.json",
        "maps/mp/" + base + ".lighting.json"
    };
    bool ok = true;
    report = "IW8 ZoneTool WASM - dump preflight\n";
    for (const auto& item : required) {
        const bool exists = fs::is_regular_file(dir / item);
        report += exists ? "[OK] " : "[MISSING] ";
        report += item + "\n";
        ok = ok && exists;
    }
    report += "\n";
    report += ok
      ? "Input files are present. This bootstrap does NOT compile native .ff files yet.\n"
      : "Dump is incomplete. Add the missing files before native compilation.\n";
    return ok ? 1 : 0;
}

EMSCRIPTEN_KEEPALIVE
const char* ztw_last_report() {
    return report.c_str();
}
}
