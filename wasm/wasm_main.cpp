#include <emscripten.h>
#include <cstdio>
#include <string>
#include <vector>
#include <filesystem>
#include <fstream>
namespace fs = std::filesystem;
static std::string g_report;
extern "C" {
EMSCRIPTEN_KEEPALIVE const char* ztw_version(){return "IW8 ZoneTool WASM 0.2 (graybox dump exporter + preflight)";}
EMSCRIPTEN_KEEPALIVE int ztw_check_dump(const char* root,const char* mapName){
 g_report.clear();
 if(!root||!mapName||!*mapName){g_report="ERROR: dump path or map name is empty.";return 0;}
 const fs::path dir(root);const std::string base=std::string(mapName)+".d3dbsp";
 const std::vector<std::string> required={std::string(mapName)+"_iw8_ents.txt","maps/mp/"+base+".render.json","maps/mp/"+base+".lighting.json"};
 bool ok=true;g_report="IW8 ZoneTool dump preflight\n";
 for(const auto& item:required){const bool exists=fs::is_regular_file(dir/item);g_report+=(exists?"[OK] ":"[MISSING] ")+item+"\n";ok=ok&&exists;}
 const bool havok=fs::is_regular_file(dir/"maps/mp"/(base+".havok"));
 g_report+=(havok?"[OK] collision Havok present\n":"[WARN] collision Havok absent — native compilation may require Replay collision baking\n");
 g_report+="\n";
 g_report+=ok?"Graybox dump structure is present. Validate its material/collision data with native ZoneTool.\n":"Dump is incomplete.\n";
 return ok?1:0;
}
EMSCRIPTEN_KEEPALIVE const char* ztw_last_report(){return g_report.c_str();}
}
