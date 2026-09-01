using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using TerrasavrNative.App.Services;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.Data;
using TerrasavrNative.Core.Model;
using TerrasavrNative.Core.Nbt;
using TerrasavrNative.Core.PlrFormat;
using TerrasavrNative.Core.WldFormat;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.DisableOptimizations | DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints | DebuggableAttribute.DebuggingModes.EnableEditAndContinue)]
[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]
[assembly: AssemblyAssociatedContentFile("assets/builds.json")]
[assembly: AssemblyAssociatedContentFile("assets/builds_calamity.json")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/best_prefix.json")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buffs.json")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/catalog.json")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/prefixes.json")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/rogue_prefixes.json")]
[assembly: AssemblyAssociatedContentFile("assets/calamity_buff_descriptions.json")]
[assembly: AssemblyAssociatedContentFile("assets/changelog.json")]
[assembly: AssemblyAssociatedContentFile("assets/hair_dyes.json")]
[assembly: AssemblyAssociatedContentFile("assets/map_colors.json")]
[assembly: AssemblyAssociatedContentFile("assets/npc_names.json")]
[assembly: AssemblyAssociatedContentFile("assets/tile_names.json")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla_buff_descriptions.json")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla_buff_names.json")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla_categories.json")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla_item_ids_by_key.json")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla_item_names.json")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla_item_names_by_key.json")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla_prefix_rules.json")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla_stats.json")]
[assembly: AssemblyAssociatedContentFile("assets/whats_new.json")]
[assembly: AssemblyAssociatedContentFile("assets/branding/logo-128.png")]
[assembly: AssemblyAssociatedContentFile("assets/branding/logo-256.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/abandonedslimebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/absorberaffliction.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/absorberregen.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/abyssaldivingsuitbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/abyssalmadness.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/adrenalinemode.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/aeolianearthbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/afflicted.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/akatobuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/alcoholpoisoning.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/amidiasblessing.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/amphibiansguitarbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/ancientmineralsharkbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/andromedabuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/andromedacripple.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/andromedasmallbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/anechoiccoatingbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/aquaticheartbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/aquaticheartwaterspeed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/aquaticstar.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/aqueoushunterdronebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/archeroflunamoon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/armorcrunch.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/astralinfectiondebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/astralinjectionbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/astralprobebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/astrophagebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/auricrebuke.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/avertorbonus.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/babybloodcrawlerbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/babyghostbellbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/babypaladinbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/babystormlionbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/baconoilbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/baguettebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/bane.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/banishingfire.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/bearbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/beldumbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/belladonnaspiritbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/blackhawkbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/blightedslime.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/blightedslimecrimson.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/bloodbound.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/bloodfinboost.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/bloodflarebloodfrenzy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/bloodymarybuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/bluecandlebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/bosseffects.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/boundingbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/brainrot.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/brimflamefrenzybuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/brimlingbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/brimrosemount.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/brimseekerbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/brimstoneelemental.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/brimstoneflames.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/brittlestar.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/bumbledogemount.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/burningblood.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/burrowerbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/calamarislamentbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/calciumbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/caribbeanrumbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/causticstaffbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/ceaselesshunger.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/chaoscandlebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/chibiidogbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/chibuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/cinderblossombuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/cinnamonrollbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/clamity.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/classicscalpetbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/cloudelemental.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/coralsymbiosis.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/corruptioneffigybuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/corvidharbringerbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/cosmicenergy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/cosmicfreeze.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/cosmicviperenginebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/cosmilampbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/crimsoneffigybuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/crumbling.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/crushdepth.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/daedaluscrystalbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/daedalusgolembuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/dankcreeperbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/dannydevito.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/daybroken.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/dazzlingstabberbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/demonicflames.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/divinebless.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/dogextremegravity.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/draedongamerchairbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/dragonfire.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/dreamfog.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/effigyofdecaybuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/eidolonsnailbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/electrictroublemaker.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/elementalmix.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/empyreanwrath.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/encased.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/enchantedknifestaffbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/endocooperbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/endohydrabuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/enraged.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/entropysvigilbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/eutrophication.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/everclearbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/evergreenginbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/exoskeletoncannons.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/exotankbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/eyeofnightbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/fierydraconidbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/fireballbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/fishalert.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/flakhermitbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/fleshballbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/flowersofmortalitybuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/foxpetbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/frostblossombuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/frostybatbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/frozenlungs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/fulfilledcontract.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/fungalclumpbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/furtasticduobuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/galvaniccorrosion.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/gammahydrabuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/gastricaberrationbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/gazeofcrysthamyrbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/glacialembracebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/godslayerinferno.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/goldiebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/grapebeerbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/gravitynormalizerbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/graxboost.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/greenjellyregen.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/hadopelagicpressure.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/hallowedrunedefense.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/hallowedrunepower.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/hallowedruneregeneration.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/haste.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/haunteddishesbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/heavybleeding.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/hermitcrab.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/herring.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/holyflames.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/holyinferno.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/hote.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/howltrio.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/hydrothermicventbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/icarusfolly.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/iceclasperbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/iceshieldbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/igneousexaltationbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/irradiated.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/kalandramirrorbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/kendra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/kingofconstellationsbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/laceration.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/ladbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/legionofcelestiabuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/levibuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/liliesoffinalitybuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/lilplaguebringerbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/littlelightbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/lordebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/magichatbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/malnourished.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/manaburn.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/manhattanbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/margaritabuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/markedfordeath.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/marniteliftbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/midnightsunbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/miniatureeyeofcthulhu.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/minimindbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/miniplaguebringerbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/miracleblight.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/moonfistbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/moonshinebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/moscowmulebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/mountedscannerbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/mushy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/mutatedtrufflebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/nightwither.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/nou.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/oasiselementalbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/oceanspiritbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/oldfashionedbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/omniscience.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/onyxexcavatorbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/pearlaura.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/perditionbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/phantom.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/phantomicempowerment.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/phantomicregen.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/phantomicshield.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/photosynthesisbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/pineapplebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/pinkcandlebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/pinkjellyregen.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/plague.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/plaguebringerbabbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/plantationstaffbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/polewarperbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/popobuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/poponoselessbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/profanedcrystalbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/profanedsoulguardians.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/profanedweakness.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/puffwarriorbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/purplecandlebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/purplehazebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/radiatorbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/ragemode.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/reaverrage.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/redwinebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/resurrectionbutterflybuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/rimehoundbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/riptidedebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/rumbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/sagepoison.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/sagespiritbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/sandelemental.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/sandnado.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/sandswindbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/sarospossessionbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/screwdriverbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/searinglava.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/seasnailbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/sentinallash.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/sepulcherminionbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/shadowbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/shadowflame.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/shellfishbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/shellfishclaps.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/shred.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/silvacrystalbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/silvarevival.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/siriusbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/skeletaldragonsbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/smallskeletonbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/smashedevil.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/snakeeyesbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/snapclamdebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/soaring.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/solargodspiritbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/solarspirit.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/soulseekerbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/sparksbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/spiritdefense.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/spiritpower.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/spiritregen.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/squishybeanbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/starbeamryebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/starswallowerpetbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/staticdischarge.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/stellartorusbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/sulphuricpoisoning.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/sulphurskinbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/swineswrathbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/tacticalplagueenginebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/tarragoncloak.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/tarragonimmunity.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/tarraliferegen.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/temporalsadness.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/tequilabuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/tequilasunrisebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/teslabuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/thecartofgodsbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/thirdsagebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/timedistortion.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/toastybatbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/tranquilitycandlebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/trippy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/truevulnerabilityhex.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/tundraflameblossomsbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/valkyriebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/vaporfied.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/vermillionflux.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/vilefeederbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/viridvanguardbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/virilibuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/vodkabuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/voidconcentrationbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/voideatermarionettebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/voidfrost.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/vulnerabilityhex.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/warped.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/waterelemental.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/weakbrimstoneflames.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/weakpetrification.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/weaponimbuebrimstone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/weaponimbuecrumbling.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/weaponimbueholyflames.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/whiskeybuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/whisperingdeath.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/whitewinebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/windchilled.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/witherblossomsbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/witherdebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/withered.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/wulfrumdroidbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/yellowcandlebuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/yharonsonbuff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/zen.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/buff_icons/zerg.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abaddon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abaddon_face.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abandonedslimestaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abandonedwulfrumhelmet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abandonedwulfrumhelmettrans_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abandonedwulfrumhelmet_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abandonedwulfrumhelmet_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abandonedwulfrumhelmet_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abombination.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/absolutezero.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abyssaldivinggear.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abyssaldivinggear_face.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abyssaldivingsuit.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abyssaldivingsuit_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abyssaldivingsuit_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abyssaldivingsuit_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abyssalmirror.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abyssaltome.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abyssaltreasure.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abyssalwarhammer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abyssbathtub.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abyssbed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abyssblade.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abyssbookcase.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abysscandelabra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abysscandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abysschair.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abysschandelier.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abysschest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abyssclock.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abyssdoor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abyssdresser.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abyssfountainitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abyssgravel.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abyssgravelwallitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abysslamp.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abysslantern.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abysslayer1musicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abysslayer2musicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abysslayer3altmusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abysslayer3musicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abysslayer4musicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abysslegacymusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abyssshellfossil.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abyssshocker.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abyssshocker_mask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abysssink.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abysssofa.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abysssynth.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abysstable.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abysstreasurechest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/abyssworkbench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acesapronofaffection.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acesapronofaffection_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aceshigh.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acideelbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidgun.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidraindye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidraintier1musicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidraintier3musicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidwood.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidwoodbathtub.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidwoodbed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidwoodbench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidwoodbookcase.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidwoodbow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidwoodcandelabra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidwoodcandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidwoodchair.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidwoodchandelier.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidwoodchest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidwoodclock.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidwooddoor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidwooddresser.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidwoodhammer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidwoodlamp.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidwoodlantern.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidwoodpiano.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidwoodplatform.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidwoodsink.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidwoodsword.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidwoodtable.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidwoodwallitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/acidwoodworkbench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/activatedauricpanel.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/adamantiteparticleaccelerator.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/adamantitethrowingaxe.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/adrenalinehairdye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/advanceddisplay.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aegisblade.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aerialhamaxe.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aerialitebar.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aerialitebrick.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aerialitebrickwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aerialitedye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aerialiteore.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aerialiteoredisenchanted.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aerialtracker.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aeroslimebanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aerospecbreastplate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aerospecbreastplate_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aerospecheadmagic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aerospecheadmagic_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aerospecheadmelee.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aerospecheadmelee_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aerospecheadranged.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aerospecheadranged_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aerospecheadrogue.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aerospecheadrogue_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aerospecheadsummon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aerospecheadsummon_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aerospecleggings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aerospecleggings_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aerostone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aestheticus.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aetherfluxcannon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aetherswhisper.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/affliction.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/afflictionglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/agedbiolightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/agedblacklightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/agedcinderlightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/agedflamelightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/agedfloodlightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/agedfrostlightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/agedlablightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/agedlaboratoryconsoleitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/agedlaboratorycontainmentboxitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/agedlaboratorydisplayitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/agedlaboratorydooritem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/agedlaboratoryelectricpanelitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/agedlaboratoryscreenitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/agedlaboratoryserveritem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/agedlaboratoryterminalitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/agedreinforcedcrateitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/agedsecuritychest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aggressivevoucher.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/airspinner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/alchemicaldecanter.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aldebaranalewife.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/algalprismtorch.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/algalprismtorch_flame.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/alluringbait.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/alluvion.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/alphadraconis.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/altaroftheaccurseditem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/alulaaustralis.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/amalgamatedbrain.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ambercrawlerbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ambrosialampoule.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/amethystcrawlerbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/amidiaspendant.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/amidiastrident.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/amphibiansguitar.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anahitamask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anahitamask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anahitamusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anahitasarpeggio.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anahitasluremusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anahitatrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anarchyblade.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientaltar.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientbasin.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientbathtub.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientbed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientbonedust.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientbookcase.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientchair.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientchandelier.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientdoor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientdresser.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientfossil.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientgodslayerchestplate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientgodslayerchestplate_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientgodslayerhelm.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientgodslayerhelm_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientgodslayerleggings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientgodslayerleggings_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancienticechunk.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientlamp.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientlantern.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientmonolith.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientnavystonebathtub.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientnavystonebed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientnavystonebench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientnavystonebookcase.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientnavystonecandelabra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientnavystonecandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientnavystonechair.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientnavystonechandelier.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientnavystonechest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientnavystoneclock.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientnavystonedoor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientnavystonedresser.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientnavystonelamp.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientnavystonelantern.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientnavystonepiano.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientnavystoneplatform.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientnavystonesink.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientnavystonetable.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientnavystoneworkbench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientpipeorgan.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientsink.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientsmoothnavystone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientsmoothnavystonewall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientsofa.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancienttable.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ancientworkbench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/androombabanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/androombaitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anechoiccoating.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anechoicplating.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/angelicalliance.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/angelicshotgun.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/angeltreads.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/angeltreads_shoes.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/animosity.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anodizedwulfrumbathtub.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anodizedwulfrumbookcase.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anodizedwulfrumchair.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anodizedwulfrumchandelier.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anodizedwulfrumchest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anodizedwulfrumclock.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anodizedwulfrumdesklight.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anodizedwulfrumdoor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anodizedwulfrumdresser.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anodizedwulfrumjukebox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anodizedwulfrumlamp.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anodizedwulfrumlantern.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anodizedwulfrummetal.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anodizedwulfrumpanels.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anodizedwulfrumplatform.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anodizedwulfrumplatform_glow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anodizedwulfrumsink.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anodizedwulfrumsleepingpod.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anodizedwulfrumsofa.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anodizedwulfrumtable.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anodizedwulfrumtoilet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anodizedwulfrumtrim.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anodizedwulfrumtrimwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anodizedwulfrumworkbench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anthozoancrabbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/anticystointment.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/antimaterielrifle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/antitumorointment.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/antlionskewer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aorta.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/apathanull.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/apoctolith.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/apoctolithglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/apoctosisarray.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/apoctosisarrayglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/apollomask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/apollomask_extra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/apollomask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/apollotrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/apollyon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/apotheosis.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/apotheosisglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aquamarinestaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aquashardshotgun.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aquasscepter.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aquaticemblem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aquaticheart.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aquaticscourgebag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aquaticscourgemask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aquaticscourgemask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aquaticscourgemusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aquaticscourgerelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aquaticscourgetrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aquatictrans_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aquatictrans_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aquatictrans_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aquaticurchinbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aqueoushunterdrone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aqueoushunterdroneglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/arbalest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/arcflashring.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/archaicpowder.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/archamaryllis.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/archerfish.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/arcnovadiffuser.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/arcticbearpaw.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/arcturusastroidean.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aresexoskeleton.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aresexoskeletonremote.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aresexoskeleton_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aresmask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aresmask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/arestrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aridartifact.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aridsoil.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ariesbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/arietes41.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/arkofthecosmos.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/arkofthecosmosglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/arkofthecosmoshandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/arkoftheelements.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/arkoftheelementsback.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/armoredshell.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/artattack.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/artemismask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/artemismask_extra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/artemismask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/artemistrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/arterialassault.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ascendantinsignia.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ascendantspiritessence.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ascendantspiritessenceglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/asgardianaegis.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/asgardianaegis_shield.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/asgardsvalor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/asgardsvalor_shield.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashenaltar.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashenbasin.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashenbathtub.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashenbed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashenbookcase.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashencandelabra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashencandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashenchair.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashenchandelier.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashenchest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashendoor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashendresser.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashenhorns.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashenhorns_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashenlamp.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashenlantern.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashenmonolith.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashenpipeorgan.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashenplatform.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashensink.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashenslab.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashenslabwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashensofa.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashenstalactite.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashentable.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashenworkbench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashesofannihilation.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ashesofcalamity.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astraglomeratebanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralachneabanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralachneastaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralbar.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralbeaconitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralblaster.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralbluedye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralbreastplate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralbreastplate_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralbrick.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralbrickwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralchest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralchunk.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralclay.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralcrate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astraldirt.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astraldirtwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astraldye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralfountainitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralgrassseeds.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralgrasswall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralhamaxe.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralhelm.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralhelm_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralice.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralicewall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralinfectionmusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralinfectionundergroundmusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralinjection.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralleggings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralleggings_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralmonolith.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralmonolithwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralorangedye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralore.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralpickaxe.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralpike.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralprobebanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralprojector.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralpylon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralsand.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralsandstone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralsandstonewall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralscythe.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralslimebanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralsnow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralsnowwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralsolution.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralstone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralstonewall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astralswirldye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astraltorch.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astraltorch_flame.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astrealdefeat.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astrophageitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astrumaureusbag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astrumaureusmask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astrumaureusmask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astrumaureusmusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astrumaureusrelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astrumaureustrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astrumdeusbag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astrumdeusmask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astrumdeusmask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astrumdeusmusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astrumdeusrelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/astrumdeustrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ataraxia.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/atlantis.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/atlasbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/atlasmunitionsbeacon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/atlasmunitionsbeaconglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auger.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/augerglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/augurofthevoid.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auralis.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auralisglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aureatebooster.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aureatebooster_wings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aureuscell.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricabsorber.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricbar.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricbar_glow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricconsole.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricdisplay.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricdye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auriclandmine.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricore.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricpanel.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricplatform.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricquantumcoolingcell.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricreinforcedcrate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricrepulserpanel.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricscreen.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricserver.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricterminal.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricteslabodyarmor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricteslabodyarmor_back.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricteslabodyarmor_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricteslacuisses.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricteslacuisses_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricteslaheadmagic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricteslaheadmagic_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricteslaheadmelee.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricteslaheadmelee_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricteslaheadranged.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricteslaheadranged_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricteslaheadrogue.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricteslaheadrogue_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricteslaheadsummon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auricteslaheadsummon_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aurictoilet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aurorablazer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/aurorablazerglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auroradicalthrow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/auroraspiritbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/avalanche.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/axeofpurity.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/babycannonballjellyfishbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/babycannonballjellyfishbowl.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/babycannonballjellyfishitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/babyflakcrabbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/babyflakcrabcage.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/babyflakcrabitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/babyghostbellbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/babyghostbellitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/babyghostbelljar.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/baconoil.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/badgeofbravery.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/baguette.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bakidon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/balefulharvester.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ballandchain.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ballandchaindisabled.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ballisticpoisonbomb.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ballofugu.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bansheehook.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bansheehookglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/barberry.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/barinade.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/barinautical.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/baroclaw.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/barracudagun.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/basalt.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/basaltslab.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/basher.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/batholithbangle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bearseye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/beastialpickaxe.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/belchingcoralbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/belchingsaxophone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/belladonnaspiritstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/berserkerwaraxe.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bige_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bige_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bige_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/biofusillade.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/biolabmusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/blackanurian.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/blackglassband.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/blackhawkremote.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/blackpearlpile.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bladecrestoathsword.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bladedgerailbow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/blasphemousdonut.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/blastbarrel.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/blazingstar.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bleachball.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/blightedcleaver.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/blightedgel.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/blightedgelred.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/blightedslimestaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/blightspewer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/blindedanglerbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/blissfulbombardier.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/blissfulbombardierglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloatfishbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodbath.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodboiler.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodfin.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodfirearrow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodfirebullet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodflarebodyarmor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodflarebodyarmor_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodflarecore.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodflarecuisses.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodflarecuisses_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodflaredye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodflareheadmagic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodflareheadmagic_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodflareheadmelee.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodflareheadmelee_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodflareheadranged.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodflareheadranged_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodflareheadrogue.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodflareheadrogue_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodflareheadsummon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodflareheadsummon_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodorb.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodpact.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodrune.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodsoakedcrasher.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodstainedglove.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodstainedglove_handsoff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodstainedglove_handson.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodstone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodwormitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodymary.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodyvein.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodywormfood.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodywormscarf.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodywormscarf_neck.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloodywormtooth.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloomslimebanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bloomstone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/blossomflux.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/blossompickaxe.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bluecosmicflamedye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bluedistortedmonolith.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bluestatigeldye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/blunderbooster.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bobbithook.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bobbitwormbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bohldohrbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bonebreaker.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/borealisbomber.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bossrushmonolith.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bossrushtier1musicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bossrushtier2musicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bossrushtier3musicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bossrushtier4musicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bossrushtier5musicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/botanicbathtub.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/botanicbed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/botanicbench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/botanicbookcase.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/botaniccandelabra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/botaniccandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/botanicchair.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/botanicchandelier.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/botanicchest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/botanicclock.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/botanicdoor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/botanicdresser.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/botaniclamp.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/botaniclantern.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/botanicpiano.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/botanicpiercer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/botanicpiercerglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/botanicplanter.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/botanicplatform.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/botanicsink.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/botanictable.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/botanicworkbench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bottledpanacea.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bouncingeyeball.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/boundingpotion.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/boxjellyfishbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brainstormtrailermusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimblade.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimflameboots.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimflameboots_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimflamecowl.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimflamecowl_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimflamedye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimflamerobes.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimflamerobes_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimlance.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimlash.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimlish.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimrose.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimrosechair_back.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimrosechair_front.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimrosestaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimstonecragsmusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimstonecrate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimstoneelementalbag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimstoneelementalmask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimstoneelementalmask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimstoneelementalmusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimstoneelementalrelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimstoneelementaltrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimstonefury.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimstonejewel.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimstonelavafountainitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimstonelocus.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimstoneslab.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimstoneslabwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimstoneslag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimstoneslagwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brimstonesword.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brinybaron.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brittlestarstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brokenbiomeblade.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brokenplaguedbed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/brokenwaterfilter.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bubonicround.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bulletfilledshotgun.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bumbledoge_back.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bumbledoge_front.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bumblefuckmask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/bumblefuckmask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/burningrevelation.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/burningrevelationglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/burningsea.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/burningstrife.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/burntsienna.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/burrowerbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/burrowercontroller.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/buzzkill.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cadaverouscarrion.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cagedbiolightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cagedblacklightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cagedcinderlightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cagedflamelightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cagedfloodlightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cagedfrostlightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cagedlablightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/calamarislament.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/calamitasclonebag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/calamitasclonemask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/calamitasclonemask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/calamitasclonemusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/calamitasclonerelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/calamitasclonetrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/calamitascoffer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/calamitasdefeatmusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/calamitasphase1musicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/calamitasphase2musicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/calamitasphase3musicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/calamitasrelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/calamitousdye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/calamity.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/calamitycanvas.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/calamityeyebanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/calamitytitlemusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/calamity_gfb.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/calciumpotion.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cannonballjellyfishbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/caribbeanrum.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/carnage.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/carnageray.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cataclysmtrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/catastrophetrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/causticcroakerstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/causticstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/caustictear.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/caustictorch.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/caustictorch_flame.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ceaselessdye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ceaselesshungerpotion.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ceaselessvoidbag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ceaselessvoidmask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ceaselessvoidmask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ceaselessvoidmusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ceaselessvoidrelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ceaselessvoidtrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/celestialclaymore.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/celestialonion.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/celestialreaper.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/celestialremains.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/celestialremainswall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/celestus.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/celestusglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ceremonialurn.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/chaliceofthebloodgod.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/chaoscandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/chaoscandle_flame.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/chaosstone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/chaoticpufferbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/chargedwulfrumenergybarrier.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/chargedwulfrumenergybarrierwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/chargedwulfrumwallmountedbulb.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/chargingstationitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/charlotte_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/charlotte_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/charlotte_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/charredidol.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/charredlasher.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/charredrelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/chickencannon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/chromaticeruption.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/chromaticeruptionglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/chromaticorb.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/chronomancersscythe.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cinderarrow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cinderblossomseeds.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cinderblossomstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cinderplate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cinderplatewall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cindersoflament.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cinnamonroll.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cinquedea.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cladcrabbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/clambanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/clamcrusher.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/clamorrifle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/claretcannon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cleansingblaze.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cleansingblazeglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cleansingjelly.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/clockworkbow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/clothierswrath.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cloudelementalbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cnidarian.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cnidarianfishingrod.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cnidrionbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/coastaldemonfish.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cobaltkunai.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/codebreakerbase.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/coinofdeceit.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/coldhearticicle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/colossalsquidbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/combatvoucher.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cometfruit.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cometquasher.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cometquasherglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cometshard.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/condemnation.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/conferencecall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/consecratedwater.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/contagion.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/contaminatedbile.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/continentalgreatbow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/coralskinfoolfish.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/coralspout.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/coreofcalamity.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/coreofcalamityglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/corinthprime.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/corpusavertor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/corrodedcaustibow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/corrodedfossil.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/corrosivespine.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/corruptioneffigy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/corvidharbringerstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmicanvilitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmicdischarge.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmicimmaterializer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmicimmaterializerglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmickunai.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmicplushie.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmicrainbow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmicshiv.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmicviperengine.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmicworm.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmilamp.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmilitebar.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmilitebasin.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmilitebathtub.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmilitebed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmilitebookcase.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmilitebrick.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmilitebrickwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmilitecandelabra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmilitecandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmilitechair.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmilitechandelier.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmilitechest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmiliteclock.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmilitedoor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmilitedresser.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmilitedye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmilitelamp.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmilitelantern.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmilitepiano.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmiliteplatform.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmilitesconce.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmilitesink.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmilitesofa.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmilitetable.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmiliteworkbench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cosmolight.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/countermeasuremitt.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/countermeasuremittglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/counterscarf.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/counterscarf_neck.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/crabulonbag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/crabulonmask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/crabulonmask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/crabulonmusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/crabulonrelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/crabulontrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/crackshotcolt.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cragbullhead.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cragmawmirerelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cragmawmiretrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cragspylon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/craniumsmasher.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/crawcarapace.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/crescentmoon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/crimsoneffigy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/crimulanblightslimebanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/crownjewel.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/crushingego.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/crushsawcrasher.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cryogenbag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cryogenicstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cryogenmask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cryogenmask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cryogenmusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cryogenrelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cryogentrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cryokey.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cryonbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cryonicbar.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cryonicbrick.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cryonicbrickwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cryonicdye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cryonicore.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cryophobia.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cryoslimebanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cryostone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/crystalcrawlerbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/crystalline.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/crystalpiercer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/crysthamyrextra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/crysthamyr_back.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/crysthamyr_front.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/crystylcrusher.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/curseddagger.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cuttlefishbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cyancoral.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/cyanseekingmechanism.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/daawnlightspiritorigin.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/daedalusbreastplate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/daedalusbreastplate_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/daedalusbreastplate_waist.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/daedalusdye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/daedalusgolemstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/daedalusheadmagic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/daedalusheadmagic_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/daedalusheadmelee.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/daedalusheadmelee_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/daedalusheadranged.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/daedalusheadranged_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/daedalusheadrogue.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/daedalusheadrogue_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/daedalusheadsummon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/daedalusheadsummon_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/daedalusleggings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/daedalusleggings_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/daemonsflame.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/daemonsflameglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dandy_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dandy_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dandy_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dankstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/darkechogreatbow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/darklightgreatsword.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/darkmattersheath.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/darkplasma.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/darkspark.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/darksunfragment.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/darksunfragmentglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/darksunring.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dazzlingstabberstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dazzlingstabberstaffglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/deadshotbrooch.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/deadsunswind.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/deathsascension.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/deathstarerod.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/deathvalleyduster.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/deathwhistle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/decapoditasprout.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/decryptioncomputer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/deepcoregk2.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/deepdiver.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/deepseaanchor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/deepseadumbbell.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/deepseastaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/deepwounder.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/defectivesphere.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/defiledflamedye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/defiledgreatsword.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/deificamulet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/deliciousmeat.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/demonshadebreastplate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/demonshadebreastplate_arms.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/demonshadebreastplate_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/demonshadegreaves.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/demonshadegreaves_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/demonshadehelm.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/demonshadehelm_extension.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/demonshadehelm_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/depthcells.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/depthcharm.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/depthcharm_waist.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/depthcrusher.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/desecratedwater.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/desertmedallion.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/desertprowlerhat.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/desertprowlerhat_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/desertprowlerpants.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/desertprowlerpants_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/desertprowlershirt.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/desertprowlershirt_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/desertprowlershirt_bulk.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/desertscourgebag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/desertscourgemask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/desertscourgemask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/desertscourgemusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/desertscourgerelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/desertscourgetrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/despairstonebanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/devilfishbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/devilsdevastation.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/devilssunrise.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/devourerofgodsbag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/devourerofgodsbagglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/devourerofgodseulogymusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/devourerofgodsmask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/devourerofgodsmask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/devourerofgodsphase1musicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/devourerofgodsphase2musicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/devourerofgodsrelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/devourerofgodstrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/diamondcrawlerbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/diamondofthedeep.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/diamondofthedeep_neck.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dimensiontearingdisk.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dimensiontearingdiskglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/disgustingmeat.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/disgustingslop.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/divinegeode.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/divineprovidence.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dogcartbody.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dogcartmount_front.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dogcarttail.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/doomsdaydevice.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/doomsdaydeviceglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/doomsdaydeviceglow2.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dormantbrimseeker.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/downpour.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/draconicdestruction.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/draconicincense.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/draconicswarmerbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/draedonbag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/draedonexoselectmusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/draedongamerchairmount_back.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/draedongamerchairmount_front.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/draedongamerchairmount_glowmask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/draedonmask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/draedonmask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/draedonpowercell.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/draedonrelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/draedonsforge.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/draedonsheart.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/draedonsloghell.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/draedonslogjungle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/draedonslogplanetoid.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/draedonslogsnowbiome.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/draedonslogsunkensea.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/draedontalkmusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dragonblooddisgorger.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dragonfollybag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dragonfollymusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dragonfollyrelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dragonfollytrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dragonpow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dragonrage.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dragonsbreath.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dragonscales.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dragonsouldye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dragoondrizzlefish.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/drataliornus.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dreadminestaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/driftwood.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/driftwoodbaroquecello.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/driftwoodbathtub.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/driftwoodbed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/driftwoodbookcase.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/driftwoodbow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/driftwoodcandelabra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/driftwoodcandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/driftwoodchair.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/driftwoodchandelier.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/driftwoodchest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/driftwooddoor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/driftwooddresser.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/driftwoodhammer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/driftwoodlamp.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/driftwoodlantern.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/driftwoodplatform.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/driftwoodsink.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/driftwoodsofa.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/driftwoodsundial.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/driftwoodsword.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/driftwoodtable.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/driftwoodtoilet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/driftwoodwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/driftwoodworkbench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dryadstear.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dubiousplating.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dukesdecapitator.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dunesand.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/duststorminabottle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dynamicpursuer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/dynamostemcells.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/earlybloomrod.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/earth.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/earthelementalbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/earthenpike.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/earthglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ebonianblightslimebanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eclipsemirror.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eclipsesfall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eclipsesfallglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ectoheart.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ectoheartglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ectoheart_animated.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/effervescence.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/effigyofdecay.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/effulgentfeather.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eidolicwail.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eidolistbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eidolonstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eidolontablet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eidolonwyrmjuvenilebanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eldendiorama.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eldritchtome.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/electriciansglove.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/electriciansglove_handsoff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/electriciansglove_handson.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/electrolytegelpack.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/elementaldye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/elementalgauntlet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/elementalgauntlet_handsoff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/elementalgauntlet_handson.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/elementalinabottle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/elephantkiller.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/elumplate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/elumplatewall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/elysianaegis.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/elysianaegis_shield.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/elysianarrow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/elysianwings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/elysianwingsglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/elysianwings_wings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/elysianwings_wingsglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/emeraldcrawlerbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/empyreancloak.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/empyreancloak_arms.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/empyreancloak_back.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/empyreancloak_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/empyreancloak_neck.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/empyreancuisses.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/empyreancuisses_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/empyreanknives.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/empyreanmask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/empyreanmask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/enchantedaxe.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/enchantedbutterfly.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/enchantedconch.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/enchantedknifestaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/enchantedpearl.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/enchantedstarfish.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/encryptedschematichell.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/encryptedschematicice.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/encryptedschematicjungle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/encryptedschematicplanetoid.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/encryptedschematicsunkensea.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/endogenesis.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/endohydrastaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/endothermicdye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/endothermicenergy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/endothermicenergyglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/energycore.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/entropicclaymore.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/entropysvigil.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/epidemicshredder.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/equanimity.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/essenceflayer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/essenceflayerglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/essenceofeleum.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/essenceofhavoc.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/essenceofsunlight.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eternalblizzard.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eternity.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/etherealcore.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/etherealextorter.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/etherealsubjugator.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/etherealsubjugatorglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/etherealtalisman.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eutrophiccrate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eutrophicglass.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eutrophicglasswall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eutrophicraybanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eutrophicsand.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eutrophicsandfish.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eutrophicsandwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eutrophicsandwallsafe.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eutrophicshelf.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/evasionscarf.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/evasionscarf_neck.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eventhorizon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/everclear.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/evergladespray.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/evergreengin.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/evilsmasher.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/evilsmasherglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eviscerator.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exaltedoathblade.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/executionersblade.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/executionersbladeglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exoarmamentskit.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exoauricpanel.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exobathtub.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exobed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exoblade.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exobladeglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exobladesquare.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exobookcase.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exocandelabra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exocandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exochair.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exochandelier.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exochest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exoclock.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exoconsole.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exodisplay.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exodiumcluster.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exodoor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exodresser.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exoduswings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exoduswings_wings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exodye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exokeyboard.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exolamp.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exolantern.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exomechsmusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exoobelisk.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exoplatform.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exoplating.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exoplatingwallitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exoplating_glow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exoprism.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exoprismpanel.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exoprismpanelwallitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exoprismpanel_glow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exoprismplatform.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exoprismplatform_glow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exorcism.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exoscreen.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exoserver.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exosink.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exosofa.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exotable.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exotank_back.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exotank_backgun.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exotank_backgunglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exotank_front.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exotank_frontglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exotank_frontlayer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exotank_frontlayerglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exoterminal.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exothrone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exoticpheromones.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exotoilet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/exoworkbench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eyeofdesolation.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eyeofmagnus.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eyeofnight.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eyeoftheaccursedbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/eyeofthestorm.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/facemelter.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fadedidolatry.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fallenpaladinshammer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fantasytalisman.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fatesreveal.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fatesrevealglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fathomswarmerarmor_tail.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fathomswarmerboots.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fathomswarmerboots_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fathomswarmerbreastplate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fathomswarmerbreastplate_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fathomswarmervisage.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fathomswarmervisage_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/faultline.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fearlessgoldfishwarriorbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fearmongergreathelm.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fearmongergreathelm_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fearmongergreaves.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fearmongergreaves_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fearmongerplatemail.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fearmongerplatemail_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/feathercrown.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/feathercrown_face.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/featherknife.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fellerofevergreens.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/feraldoublerod.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/feralthornclaymore.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fetidemesis.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/filthyglove.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/filthyglove_handsoff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/filthyglove_handson.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fireball.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/firestormcannon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fireturret.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fishboneboomerang.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fishofeleum.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fishofflight.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fishstocks.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/flakcrabbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/flakkraken.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/flaktoxicannon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/flamelickedshell.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/flamsteedring.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/flarebolt.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/flarefrostblade.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/flarewingbow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/flashround.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/flaskofbrimstone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/flaskofcrumbling.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/flaskofholyflames.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fleshofinfidelity.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fleshtotem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fleshygeode.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/floodtide.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/floralwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/flowersofmortality.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/flurrystormcannon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/follyfeed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/forbiddencirclet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/forbiddencirclet_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/forbiddenoathblade.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/forbiddensun.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/forgivenesspainting.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/forgottenapexwand.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/forgottendragonegg.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/forsakensaber.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/foxdrive.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fracturedark.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fracturedarkglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fragmentsofanotherworld.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/freedomstar.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/frigidflashbolt.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/frigidmonolith.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/frogfishbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/frostbarrier.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/frostbiteblaster.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/frostblossomstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/frostbolt.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/frostcrushvalari.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/frostflare.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/frostybatbottle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/frostyflare.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/frozencube.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fuelcellbundle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fungalclump.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fungalsymbiote.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fungicide.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/fusionfeederbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gacruxianmollusk.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gaelsgreatsword.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/galactusblade.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/galactusbladeglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/galaxia.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/galaxiadawn.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/galaxiadawnoutline.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/galaxiadusk.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/galaxiaduskoutline.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/galaxiaextra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/galaxiaextra2.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/galaxysmasher.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/galeforce.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/galileogladius.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/galvanizingglaive.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gammaheart.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gammaslimebanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gastricbelcherstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gatlinglaser.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gazeofcrysthamyr.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/geldart.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/geliticblade.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gelpick.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gemtechbodyarmor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gemtechbodyarmor_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gemtechheadgear.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gemtechheadgear_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gemtechschynbaulds.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gemtechschynbaulds_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/genesis.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/genesispickaxe.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/geysershell.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ghastlyvisage.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ghastlyvisageglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ghostbellbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ghostbracelet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ghoulishgouger.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ghoulishgougerglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/giantclamrelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/giantclamtrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/giantpearl.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/giantshell.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/giantshell_back.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/giantsquidbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gianttortoiseshell.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gildeddagger.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gildedproboscis.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/glacialembrace.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gladiatorslocket.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/glaive.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gleamingcucumber.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gleamingdagger.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gleamingmagnolia.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/glimmeringgemfish.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/glimmeringribbon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gloomtorch.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gloomtorch_flame.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gloriousend.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gloveofprecision.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gloveofprecision_handsoff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gloveofprecision_handson.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gloveofrecklessness.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gloveofrecklessness_handsoff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gloveofrecklessness_handson.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gluttonyblender.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gnasherbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/godslayerchestplate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/godslayerchestplate_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/godslayerchestplate_bodyglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/godslayerheadmelee.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/godslayerheadmelee_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/godslayerheadmelee_headglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/godslayerheadranged.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/godslayerheadranged_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/godslayerheadranged_headglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/godslayerheadrogue.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/godslayerheadrogue_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/godslayerheadrogue_headglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/godslayerhornedhelm.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/godslayerhornedhelm_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/godslayerhornedhelm_headglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/godslayerleggings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/godslayerleggings_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/godslayerleggings_legsglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/godslayerslug.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/godslayervisage.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/godslayervisage_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/godslayervisage_headglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/godsparanoia.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/goldeneagle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/goldplumespear.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/goobow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gorecodile.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/granddad.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/granddadglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/grandgelatin.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/grandguardian.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/grandguardianglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/grandmarquisbait.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/grandscale.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/grapebeer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gravegrimreaver.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gravitynormalizerpotion.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/grax.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/greatbaypickaxe.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/greatsandsharkrelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/greatsandsharktrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/greatswordofjudgement.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/greatswordofjudgementglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/greedpot.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/greenseekingmechanism.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/greentide.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/greenwaveloach.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gruesomeeminence.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/guidelightofoblivion.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gulpereelbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/gunkshot.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hadalmantle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hadalmantle_wings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hadalstew.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hadalurn.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hadarianbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hadarianwings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hadarianwings_wings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hailstormbullet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/halibutcannon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/halleysinferno.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hallowedore.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hallowedrune.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hallowpointround.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/handheldtank.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hapufruit.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hardenedastralsand.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hardenedastralsandwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hardenedeutrophicsand.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hardenedhoneycomb.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hardenedsulphuroussandstone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hardenedsulphuroussandstonewall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/harpyring.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/harveststaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hauntedscroll.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/havocfish.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/havocplate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/havocplatewall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/havocsbreath.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hazardchevronpanels.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hazardchevronwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/heartofdarkness.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/heartoftheelements.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/heart_back.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/heart_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/heart_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/heart_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/heatspiritbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/heavenfallenstardisk.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/heavenlygale.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/heavenlygaleglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/heliumflash.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hellborn.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hellfireflamberge.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hellionflowerspear.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hellkite.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hellkiteglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hellwingstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/helstorm.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hematemesis.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/heresy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hermitsboxofonehundredmedicines.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/heronrod.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/herringstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hideofastrumdeus.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hivemindbag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hivemindmask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hivemindmask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hivemindmusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hivemindrelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hivemindtrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hivepod.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hoarfrostbow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/holofibreimmolator.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/holofibreimmolatorglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/holycollider.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/holycolliderglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/holyfirebullet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/honeydew.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hoodofcalamity.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hoodofcalamity_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/horriblehogbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/howlsheart.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hurriedvoucher.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hydra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hydraulicvoltcrasher.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hydrothermalcrate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hydrothermicarmor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hydrothermicarmor_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hydrothermicheadmagic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hydrothermicheadmagic_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hydrothermicheadmelee.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hydrothermicheadmelee_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hydrothermicheadranged.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hydrothermicheadranged_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hydrothermicheadrogue.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hydrothermicheadrogue_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hydrothermicheadsummon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hydrothermicheadsummon_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hydrothermicsubligar.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hydrothermicsubligar_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hyperdeathriftscepter.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hyperdeathriftscepterglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hyperiusbullet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hyphaerod.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/hypothermia.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/icebarrage.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/icebreaker.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/iceclasperbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/icestar.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/iceturret.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ichorspear.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/iciclearrow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/iciclestaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/icicletrident.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/igneousexaltation.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/illustriousknives.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ilmerisspark.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/impiousimmolatorbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/infectedarmorplating.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/infectedjewel.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/infectedremote.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/infernacutter.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/infernalblood.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/infernalcongealmentbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/infernalkris.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/infernalrift.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/infernalsuevite.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/infestedclawmerang.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/infinity.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/inkbomb.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/insidiousimpaler.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/interlude1musicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/interlude2musicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/interlude3musicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/interstellarstompers.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/invisibledye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ionblaster.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ionblasterglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ironball.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ironboots.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ironboots_shoes.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ironfrancisca.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/irradiatedslimebanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ivdripontherocks.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/jackfruit.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/jawsofoblivion.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/jellychargedbattery.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/joyfulheart.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/karasawa.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/keelhaul.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/kelptorch.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/kelptorch_flame.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/kelvincatalyst.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/kingofconstellationstenryu.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/kingsbane.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/kylie.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/labhologramprojectoritem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/laboratoryconsoleitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/laboratorycontainmentboxitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/laboratorydisplayitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/laboratorydooritem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/laboratoryelectricpanelitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/laboratorypanels.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/laboratorypanelwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/laboratorypipeplating.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/laboratoryplatebeam.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/laboratoryplatepillar.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/laboratoryplating.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/laboratoryplatingwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/laboratoryscreenitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/laboratoryserveritem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/laboratoryshelf.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/laboratoryterminalitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/labseekingmechanism.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/labturret.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lacerator.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lanterncenter.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/largeritualcandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/laserfishbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/laserturret.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lashesofchaos.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/laudanum.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lavachickenbroth.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/leadtomahawk.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/legionofcelestia.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/legionofcelestiaglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lemonnade.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/leonidprogenitor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/leonidprogenitorglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/levi.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/leviathanambergris.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/leviathananahitarelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/leviathanbag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/leviathanmask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/leviathanmask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/leviathanmusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/leviathanteeth.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/leviathantrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/leviatitan.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lifealloy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lifehuntscythe.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lifejelly.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lightgodsbrilliance.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lightspeed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lightspeedglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/liliesoffinality.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/limecoral.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/limestone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/limestonewall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lionfish.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lionheart.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/littlee.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/littlelight.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/livingbrimstonefireblock.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/livingdew.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/livinggodslayerfireblock.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/livingholyfireblock.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/livingplaguefireblock.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/livingshard.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/livingsharddye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/longrangedsensorarray.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loreabyss.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loreaquaticscourge.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lorearchmage.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loreastralinfection.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loreastrumaureus.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loreastrumdeus.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loreawakening.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loreazafure.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lorebloodmoon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lorebrainofcthulhu.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lorebrimstoneelemental.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lorecalamitas.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lorecalamitasclone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loreceaselessvoid.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lorecorruption.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lorecrabulon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lorecrimson.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lorecynosure.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loredesertscourge.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loredestroyer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loredevourerofgods.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loredragonfolly.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loredukefishron.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loreeaterofworlds.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loreempressoflight.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loreexomechs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loreeyeofcthulhu.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loregolem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lorehivemind.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lorekingslime.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loreleviathananahita.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loremechs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loreoldduke.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loreperforators.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loreplaguebringergoliath.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loreplantera.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lorepolterghast.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loreprelude.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loreprofanedguardians.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loreprovidence.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lorequeenbee.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lorequeenslime.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loreravager.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lorerequiem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loresignus.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loreskeletron.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loreskeletronprime.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loreslimegod.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lorestormweaver.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loresulphursea.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loretwins.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loreunderworld.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lorewallofflesh.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/loreyharon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lotus.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lucisboots.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lucisboots_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lucishairstyle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lucishairstyle_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lucismilitaryuniform.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lucismilitaryuniform_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lucissight.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lucissight_face.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lucrecia.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lumenyl.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/luminouscorvinabanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lunarianbow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/lunarkunai.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/luniccorpsboots.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/luniccorpsboots_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/luniccorpshelmet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/luniccorpshelmet_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/luniccorpsvest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/luniccorpsvest_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/luniceye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/luxorsgift.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/m1garand.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/madalchemistscocktailglove.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/magentacoral.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/magnacannon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/magneticmeltdown.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/magnomalycannon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/majesticguard.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/majesticguardglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/malachite.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/malevolence.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/manapolarizer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/manarose.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/mangosteen.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/mangrovechakram.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/manhattan.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/mantisbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/mantisclaws.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/mantisshrimpbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/margarita.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/markofprovidence.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marksmanbow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marksmanround.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marnitearchitectheadgear.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marnitearchitectheadgear_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marnitearchitecttoga.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marnitearchitecttoga_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marnitearchitecttoga_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marnitebathtub.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marnitebed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marnitebookcase.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marnitecandelabra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marnitecandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marnitechair.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marnitechandelier.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marnitechest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marniteclock.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marnitedeconstructor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marnitedeconstructorbloom.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marnitedeconstructorselection.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marnitedoor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marnitedresser.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marnitelamp.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marnitelantern.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marniteliftfire.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marnitelift_front.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marnitelight.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marnitelight_flame.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marniteobliterator.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marniteobliteratorbloom.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marniteorgan.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marniterepulsionshield.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marniterepulsionshield_back.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marnitesink.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marnitesofa.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marnitetable.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marnitetoilet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/marniteworkbench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/martiandistressremote.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/martiandistressremote_animated.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/maulerrelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/maulertrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/mawofinfinity.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/mawofinfinityglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/mcnuggets.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/megalodon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/meldblob.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/melddye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/meldtransformation_arms.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/meldtransformation_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/meldtransformation_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/meldtransformation_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/meldtransformation_neck.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/melterbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/meowthrower.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/metalmonstrosity.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/metastasis.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/meteorfist.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/miasma.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/midasprime.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/midnightsunbeacon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/mineralmortar.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/miniagedbiolightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/miniagedblacklightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/miniagedcinderlightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/miniagedflamelightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/miniagedfloodlightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/miniagedfrostlightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/miniagedlablightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/minicagedbiolightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/minicagedblacklightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/minicagedcinderlightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/minicagedflamelightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/minicagedfloodlightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/minicagedfrostlightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/minicagedlablightitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/miraclefruit.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/miraclematter.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/miragejellybanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/miragemirror.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/mirrorblade.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/mirrorofkalandra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/mishiro_back.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/mishiro_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/mishiro_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/mishiro_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/mistlestorm.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/moab.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/moab_wings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/molluskhusk.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/molluskshelleggings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/molluskshelleggings_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/molluskshellmet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/molluskshellmet_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/molluskshellplate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/molluskshellplate_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/moltenamputator.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/moltenfishron.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/momentumcapacitor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/monolithamalgam.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/monolithbathtub.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/monolithbed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/monolithbench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/monolithbookcase.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/monolithbow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/monolithcandelabra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/monolithcandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/monolithchair.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/monolithchandelier.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/monolithchest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/monolithclock.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/monolithcrate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/monolithdoor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/monolithdresser.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/monolithhammer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/monolithlamp.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/monolithlantern.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/monolithoftheaccursed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/monolithpiano.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/monolithplatform.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/monolithsink.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/monolithsword.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/monolithtable.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/monolithworkbench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/monsoon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/monstrousknives.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/moonshine.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/moonstonecrown.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/moonstonecrown_face.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/moonwalkers.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/moonwalkers_shoes.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/moonwalkers_wings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/morayeelbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/mortarround.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/moscowmule.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/mossystone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/mountedscanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/mourningstar.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/murasama.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/murasamaglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/murasamasheathed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/mushroomplasmaroot.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/mutatedtruffle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/mycelialclaws.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/mycoroot.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/mysteriouscircuitry.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/mythrilknife.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nadir.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/naiadswarhorn.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nanoblackreaper.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nanodroiddysfunctionalitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nanodroiditem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nanodroidplaguegreenitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nanodroidplaguereditem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nanopurge.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nanotech.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nastycholla.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/navyfishingrod.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/navyplate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/navyplatewall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/navyprismtorch.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/navyprismtorch_flame.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/navystone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/navystonebathtub.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/navystonebed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/navystonebookcase.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/navystonecandelabra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/navystonecandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/navystonechair.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/navystonechandelier.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/navystonechest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/navystoneclock.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/navystonedoor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/navystonedresser.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/navystonelamp.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/navystonelantern.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/navystonelyre.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/navystoneplatform.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/navystonesink.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/navystonetable.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/navystonetoilet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/navystonetriclinium.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/navystonewall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/navystoneworkbench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nebulash.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nebulouscataclysm.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nebulouscore.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/necklaceofvexation.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/necromanticdye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/necromanticgeode.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/necroplasm.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/necroplasmicbeacon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/necroplasmicbeaconglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/necroplasmicdye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/needler.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/neptunesbounty.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nettlevinegreatbow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nidhogg.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nidhoggglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nightmaredye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nightmarefuel.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nightmarefuelglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nightsray.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nitroexpressrifle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/no.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/norfleet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/normalityrelocator.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/novabanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/novaeslag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nuclearfuelrod.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nuclearfury.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nuclearterrorrelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nuclearterrortrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nucleartoadbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nucleogenesis.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nucleosynthesis.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nucleosynthesisglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/nullificationpistol.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/oarfishbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/oasiselementalinabottle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/oblivion.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/occultbrickitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/occultbrickwallitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/occultlegionnairebanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/occultplatformitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/occultskullcrown.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/occultskullcrown_face.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/oceancrest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/oddmushroom.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/oddvoucher.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/olddie.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/olddukebag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/olddukemask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/olddukemask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/olddukemusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/olddukerelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/olddukescales.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/oldduketrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/oldfashioned.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/oldhunterhat.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/oldhunterhat_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/oldhunterpants.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/oldhunterpants_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/oldhuntershirt.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/oldhuntershirt_back.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/oldhuntershirt_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/oldhuntershirt_bulk.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/oldlordclaymore.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/oldlordclaymoreglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/omegabiomeblade.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/omegabiomebladeextra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/omegabluechestplate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/omegabluechestplate_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/omegabluehelmet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/omegabluehelmet_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/omegabluehelmet_headmadness.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/omegabluetentacles.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/omegabluetentacles_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/omegahealingpotion.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/omicron.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/omniblade.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/omnigun.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ontologicaldespoiler.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/onyxchainblaster.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/onyxexcavatorextra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/onyxexcavatorextra2.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/onyxexcavatorkey.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/onyxexcavator_back.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/onyxexcavator_front.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/onyxia.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/onyxplate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/onyxplatewall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/onyxseekingmechanism.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/onyxturret.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/opalstriker.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/oracleheadphones.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/orangecoral.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/orderbringer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/orderbringerglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/orichalcumspikedgemstone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ornateshield.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ornateshield_shield.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/orthocerabanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/orthocerashell.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/otherworldlybathtub.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/otherworldlybed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/otherworldlybookcase.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/otherworldlycandelabra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/otherworldlycandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/otherworldlychair.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/otherworldlychandelier.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/otherworldlychest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/otherworldlyclock.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/otherworldlydoor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/otherworldlydresser.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/otherworldlylamp.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/otherworldlylantern.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/otherworldlypiano.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/otherworldlyplatform.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/otherworldlysink.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/otherworldlysofa.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/otherworldlystone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/otherworldlystonewall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/otherworldlytable.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/otherworldlyworkbench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/overloadedblaster.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/overloadedsludge.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/overloadedsoldierbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ozzathoth.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/p90.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/palladiumjavelin.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/pandemic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/parasiticsceptor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/pearlgod.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/pearlofenthrallment.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/pearlshard.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/penumbra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/perdition.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/perennialbar.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/perennialbrick.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/perennialbrickwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/perennialore.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/perennialslimebanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/perfectdark.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/perforatorbag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/perforatormask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/perforatormask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/perforatorsmusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/perforatorsrelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/perforatortrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/permafrostsconcoction.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/pestilentdefiler.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/pestilentslimebanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/phalanxsurge.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/phalanxsurgeglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/phantasmalfury.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/phantasmalruin.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/phantomheart.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/phantomicartifact.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/phantomspiritbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/phaseslayer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/phoenixflamebarrage.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/phosphorescentgauntlet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/photonripper.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/photosynthesis.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/photosynthesisglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/photosynthesispotion.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/photoviscerator.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/photovisceratorglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/piggybanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/piggycage.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/piggycagegold.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/piggygolditem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/piggyitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/pineapplepet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/pinkcosmicflamedye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/pinkpearlpile.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/pinkstatigeldye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguebringerbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguebringercarapace.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguebringercarapace_back.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguebringercarapace_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguebringergoliathbag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguebringergoliathmask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguebringergoliathmask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguebringergoliathmusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguebringergoliathrelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguebringergoliathtrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguebringerpistons.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguebringerpistons_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguebringervisor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguebringervisor_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguecaller.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguecellcanister.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguechargerbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguecontainmentcellswall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguedcontainmentbrick.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguedfuelpack.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguedplatebathtub.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguedplatebed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguedplatebookcase.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguedplatecandelabra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguedplatecandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguedplatechair.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguedplatechandelier.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguedplatechest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguedplateclock.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguedplatedoor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguedplatedresser.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguedplatelamp.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguedplatelantern.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguedplatepiano.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguedplateplatform.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguedplatesink.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguedplatesofa.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguedplatetable.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguedplatewall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguedworkbench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguegoodye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguehumidifier.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plagueinfuser.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguenade.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plagueplate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plagueplatedye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguereapermask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguereapermask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguereaperstriders.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguereaperstriders_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguereapervest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguereapervest_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plagueshellbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguestaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plaguetaintedsmg.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plagueturret.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/planebreakerspouch.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/planetaryannihilation.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/planetoidmusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plantationstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plantationstaffglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plantymush.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plasmacaster.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plasmadrivecore.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plasmagrenade.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plasmarifle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/plasmarod.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/polarisparrotfish.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/polewarper.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/polishedmarniteblock.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/polishedmarniteplatform.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/polishedmarnitewall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/polishednavystonebrick.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/polterghastbag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/polterghastbagglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/polterghastmask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/polterghastmask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/polterghastmusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/polterghastrelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/polterghasttrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/polyplauncher.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/polypsand.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/popo.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/poponoseless_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/popo_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/popo_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/popo_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/portabulb.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/poseidon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/potionofomniscience.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/powercellfactoryitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/pridefulhuntersplanarripper.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/primordialancient.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/primordialearth.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/primordialwyrmmusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/primrosekeepsake.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/prismalline.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/prismaticbreaker.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/prismaticbreakerglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/prismaticgreaves.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/prismaticgreaves_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/prismaticguppy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/prismatichelmet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/prismatichelmet_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/prismaticregalia.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/prismaticregalia_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/prismbackbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/prismcrate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/prismshard.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/pristinefury.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/pristinefuryglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/pristinefury_animated.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/procyonidprawn.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedbathtub.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedbed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedbench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedbookcase.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedcandelabra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedcandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedchair.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedchandelier.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedchest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedclock.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedcore.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedcrucible.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedcrystal.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedcrystaldye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedcrystalwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profaneddoor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profaneddresser.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedenergybanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedflamedye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedguardianmask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedguardianmask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedguardiansmusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedguardiansrelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedguardiantrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedlamp.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedlantern.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedmoonlightdye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedpartisan.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedpiano.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedplatform.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedrock.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedrockdye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedrockwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedshard.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedsink.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedslab.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedslabwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedsoulartifact.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedsoulcrystal.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedsoultransnight_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedsoultransnight_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedsoultransnight_wings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedsoultrans_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedsoultrans_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedsoultrans_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedsoultrans_wings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedtable.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/profanedworkbench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/protolithbangle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/providencebag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/providencemask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/providencemask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/providencemusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/providencerelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/providencetrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/puffshroom.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/pulsedragon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/pulsegrenade.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/pulsegrenadeglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/pulsepistol.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/pulserifle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/pulserifleglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/pulseturretremote.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/pumpkaboom.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/pumpler.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/purgeguzzler.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/purifiedgel.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/purpledistortedmonolith.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/purplehaze.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/pwnagehammer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/pyremantle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/pyremantlemolten.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/pyremantlewall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/quagmire.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/qualityslop.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/quiverofnihility.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/quiverofnihility_back.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/radiance.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/radiantooze.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/radiantstar.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/radiatingcrystal.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/radiatorbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ragebait.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ragehairdye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/raiderstalisman.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rainbowpartycannon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rampartofdeities.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rampartofdeities_shield.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rancor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ravagerbag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ravagermask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ravagermask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ravagermusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ravagerrelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ravagertrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/realityrupture.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/realmravager.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/reapersharkbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/reapertooth.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/reapertoothnecklace.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/reavercuisses.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/reavercuisses_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/reaverdye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/reaverheadexplore.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/reaverheadexplore_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/reaverheadmobility.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/reaverheadmobility_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/reaverheadtank.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/reaverheadtank_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/reaverscalemail.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/reaverscalemail_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/reboundingrainbow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/recitationofthebeast.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/redlightningcontainer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/redlightningcontainerglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/redlightningcontainer_animated.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/redseekingmechanism.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/redsun.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/redwine.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/reedblowgun.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/reefclawhamaxe.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/refractionrotor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/refractiveprismtorch.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/refractiveprismtorch_flame.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/regenerator.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/regulusriot.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/reinforcedcrateitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/relicofconvergence.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/relicofdeliverance.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/relicofresilience.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/relicofruin.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/remsrevenge.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/renegadewarlockbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/repairunitbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/repairunitcage.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/repairunititem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/resilientcandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/respiteblock.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/resurrectionbutterfly.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/riftburst.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/riftburstglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/riftreeler.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rimehoundbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rimehoundmount_back.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rimehoundmount_front.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/riptide.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ritualcandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/robesofcalamity.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/robesofcalamity_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/robesofcalamity_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rock.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rogueemblem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/romajedaorchid.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rosestone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rotball.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rotdogbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rottenbrain.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rottendogtooth.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rottingeyeball.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rougeslash.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/roundedanodizedwulfrumpanels.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/roundedanodizedwulfrumpanelwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/roverdrive.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/roxcalibur.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rubbermortarround.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rubicoprime.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rubycrawlerbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ruinmedallion.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ruinoussoul.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rum.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/runestone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/runestoneterracotta.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/runestonewall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/runicprofanedbrick.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/runicprofanedbrickwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rustedjinglebell.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rustedpipes.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rustedplatebeam.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rustedplatepillar.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rustedplating.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rustedplatingwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rustedshelf.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rustybeaconprototype.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/rustychest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sacredstrawberry.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sacrifice.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sacrilegiousbathtub.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sacrilegiousbed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sacrilegiousbench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sacrilegiousbookcase.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sacrilegiouscandelabra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sacrilegiouscandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sacrilegiouschair.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sacrilegiouschandelier.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sacrilegiouschest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sacrilegiousclock.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sacrilegiousdoor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sacrilegiousdresser.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sacrilegiouslamp.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sacrilegiouslantern.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sacrilegiousorgan.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sacrilegioussink.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sacrilegioustable.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sacrilegiousworkbench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/saharaslicers.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/salak.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/samsaraslicer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/samsaraslicerglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sanctifiedspark.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sandblaster.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sandcloak.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sanddollar.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sandsharknadostaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sandsharktoothnecklace.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sandstormscore.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sandstreamscepter.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sanguineflare.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sanguinetangerine.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sapphirecrawlerbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sarospossession.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sausagemaker.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/scabripper.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/scalboots.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/scalboots_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/scalmask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/scalmask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/scalrobes.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/scalrobes_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/scarletdevil.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/scionscurio.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/scorchedbone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/scorchedbonewall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/scorchedearth.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/scorchedremains.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/scoriabar.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/scoriabrick.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/scoriabrickwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/scoriadye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/scoriaore.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/scorneaterbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/scorpio.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/scorpio_glow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/scourgeofthecosmos.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/scourgeofthedesert.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/scourgeoftheseas.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/screwdriver.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/scryllarbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/scuttlersjewel.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sdfmg.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/seadragon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/seafloatybanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/seafoambomb.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/seafood.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sealedsingularity.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/seaminnowbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/seaminnowitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/seaminnowjar.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/seaprism.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/seaprismbrick.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/searemains.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/seaserpentbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/seashinesword.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/seaspiritamulet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/seaspiritamulet_neck.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/seassearing.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/securitychest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/seekingscorcher.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/seethingdischarge.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/seismichampick.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/septicskewer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/seraphim.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/seraphtracers.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/seraphtracers_wings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/serpentine.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/serpentsbite.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/serpentuna.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shadecrystalbarrage.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shaderainstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shadethrower.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shadowboltstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shadowfish.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shadowpotion.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shadowspecbar.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shadowspecdye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shardlightpickaxe.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shardofantumbra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shardofantumbraghost.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sharkyplush.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shark_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shark_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shark_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shatteredcommunity.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shattereddawn.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shellfishstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shellshooter.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shellstone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shellstoneslab.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shellstoneslabwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shellstonewall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shelltrimbrick.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shelltrimbrickwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shieldofthehighruler.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shieldofthehighruler_shield.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shieldoftheocean.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shiftingsands.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shimmerspark.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shinobiblade.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shockstormshuttlebanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shortcircuit.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shpc.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shredder.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shroomblebanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shroomblecage.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shroombleitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shroombowl.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/shroomer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sightseercolliderbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sightseerspitterbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sigilofcalamitas.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/signusbag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/signusmask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/signusmask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/signusmusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/signusrelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/signustrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silencingsheath.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvaarmor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvaarmor_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvabasin.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvabathtub.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvabed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvabench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvabookcase.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvacandelabra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvacandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvachair.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvachandelier.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvachest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvaclock.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvacrystal.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvadoor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvadresser.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvadye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvaheadmagic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvaheadmagic_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvaheadsummon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvaheadsummon_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvahelm.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvahelm_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvahornedhelm.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvahornedhelm_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvalamp.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvalantern.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvaleggings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvaleggings_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvamask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvamask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvapiano.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvaplatform.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvasink.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvatable.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvawall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvawings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvawings_wings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/silvaworkbench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sirenproofearmuffs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sirius.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/skyfinbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/skyfinbombers.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/skyfringepickaxe.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/skyglaze.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/skylinewings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/skylinewings_wings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/skynamite.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/skytidedragoon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/skytidedragoonglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/slabcrabbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/slagcrate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/slagfiredouser.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/slagmagnum.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/slagsplitterpauldron.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/slimegodbag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/slimegoddye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/slimegodmask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/slimegodmask2.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/slimegodmask2_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/slimegodmask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/slimegodmusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/slimegodrelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/slimegodtrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/slimepuppetstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/slitheringeels.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sludgesplotch.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/slurperpole.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/slurpfish.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/smokingcomet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/smoothabyssgravel.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/smoothabyssgravelplatform.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/smoothabyssgravelwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/smoothbrimstoneslag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/smoothbrimstoneslagwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/smoothnavystone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/smoothnavystonewall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/smoothvoidstone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/smoothvoidstoneplatform.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/smoothvoidstonewall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/snakeeyes.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/snapclam.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/snowruffianchestplate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/snowruffianchestplate_back.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/snowruffianchestplate_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/snowruffianchestplate_neck.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/snowruffiangreaves.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/snowruffiangreaves_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/snowruffianmask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/snowruffianmask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/snowruffianwings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/snowstormstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/soaringpotion.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/solarveil.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/solsticeclaymore.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/somaprime.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/soulharvester.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/soulofcryogen.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/soulofcryogen_wings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/soulpiercer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/soulpiercerglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/soulslurperbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/spadefish.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sparklingempress.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sparkspreader.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/spearofdestiny.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/spearofpaleolith.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/spectralstormcannon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/spectralveil.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/specularsturgeon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/speedblaster.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/spelunkersamulet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/spentfuelcontainer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/spikecragstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/spineofthanatos.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/spinesapling.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/spiritglyph.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/spitefulcandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sporeknife.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/springstool.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sproutingarrow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/spyker.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/squidoom.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/squirrelsquirestaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/squishybean_back.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/staffofblushie.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/staffofnecrosteocytes.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/starbeamrye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/starblightsoot.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/starbustercore.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/starcore.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/starfleet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/starfleetglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/starlightfuelcell.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/starlightwings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/starlightwings_wings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/starmada.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/starmadaglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/starmageddon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/starmageddonglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/starnightlance.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/starofdestruction.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/starofdestructionghost.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/starshower.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/starspawnhelixstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/starsputter.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/starstruckwater.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/starswallowercontainmentunit.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/starswallowerunit.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/startaintedgenerator.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/starterbag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/staticrefiner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelarmor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelarmor_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelbathtub.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelbed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelblock.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelbookcase.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelcandelabra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelcandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelchair.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelchandelier.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelchest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelclock.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigeldoor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigeldresser.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelgreaves.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelgreaves_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelheadmagic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelheadmagic_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelheadmelee.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelheadmelee_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelheadranged.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelheadranged_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelheadrogue.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelheadrogue_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelheadsummon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelheadsummon_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigellamp.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigellantern.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelpiano.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelplatform.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelsink.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelsofa.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigeltable.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statigelworkbench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statisblessing.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statisblessing_neck.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statiscurse.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statiscurse_neck.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statisninjabelt.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statisvoidsash.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/statmeter.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stealthhairdye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stellarcannon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stellarcontempt.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stellarculexbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stellarknife.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stellarstriker.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stellartorusstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stohne.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stormfrontrazor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stormjawstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stormlionbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stormlionmandible.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stormruler.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stormsaber.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stormsurge.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stormweaverbag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stormweavermask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stormweavermask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stormweavermusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stormweaverrelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/strangeorb.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stratusbathtub.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stratusbed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stratusbookcase.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stratusbricks.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stratuscandelabra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stratuscandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stratuschair.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stratuschandelier.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stratuschest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stratusclock.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stratusdoor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stratusdresser.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stratusdye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stratuslamp.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stratuslantern.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stratuspiano.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stratusplatform.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stratussink.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stratussofa.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stratussphere.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stratusstarplatformitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stratustable.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stratuswall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stratusworkbench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/streamgouge.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/streamgougeglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stresspills.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stuffedfish.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stygianshield.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/stygianshield_shield.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/subductionslicer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/submarineshocker.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/subsumingvortex.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/subsumingvortexglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/subsumingvortexsmall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/subsumingvortexsmallglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulflounderbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulphuricacidcannon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulphuricscale.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulphurictreasure.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulphurousbreastplate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulphurousbreastplate_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulphurouscrate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulphurousfountainitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulphurousgrabber.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulphuroushelmet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulphuroushelmet_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulphurousleggings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulphurousleggings_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulphuroussand.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulphuroussandstone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulphuroussandstonewall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulphuroussandwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulphurousseadaymusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulphurousseanightmusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulphuroussearainmusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulphurousseaworldsidechanger.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulphurousshale.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulphurousshalewall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulphurousskaterbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulphuroustorch.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulphuroustorch_flame.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulphurpylon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sulphurskinpotion.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sunbeamfish.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sunkenpylon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sunkensailfish.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sunkenseafountain.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sunkenseamusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sunskaterbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sunspiritstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/superdummy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/supernova.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/supernovaglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/superradiantslaughterer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/supremebaittackleboxfishingstation.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/supremecalamitastrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/supremecataclysmtrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/supremecatastrophetrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/supremehealingpotion.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/suprememanapotion.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/surfclam.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/surgedriver.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/suspiciouslookingjellybean.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/suspiciouslookingnou.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/suspiciousscrap.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/svantechnical.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/swirlingcosmicflamedye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/swordsplosion.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sylvestaffgold.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/sylvestaffplatinum.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/systembane.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tacticalplagueengine.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tacticianstrumpcard.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/taintedblade.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/taintedcloudberry.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tarragonbreastplate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tarragonbreastplate_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tarragondye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tarragonheadmagic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tarragonheadmagic_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tarragonheadmelee.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tarragonheadmelee_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tarragonheadranged.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tarragonheadranged_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tarragonheadrogue.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tarragonheadrogue_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tarragonheadsummon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tarragonheadsummon_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tarragonleggings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tarragonleggings_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tarragonthrowingdart.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tarragonwings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tarragonwings_wings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/taucannon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/taucannon_glow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/teardropcleaver.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tearsofheaven.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tectonictruncator.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/telluricglare.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/telluricglareglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/temporalumbrella.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tenebreustides.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tequila.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tequilasunrise.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/teratoma.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/terminus.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/terminusglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/terminus_gfb.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/terratomere.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/terrorblade.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/terrorbladeglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/terrortalons.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/teslacannon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/teslasamulet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/teslastaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thanatosmask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thanatosmask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thanatostrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thankyoupainting.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thaumaticchair.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/theabsorber.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/theamalgam.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/theanomalysnanogun.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/theatomsplitter.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/theballista.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thebee.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/theburningsky.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thecamper.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thecamper_back.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thecartofgods.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thecauldron.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thecauldronglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thecomb.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thecommanderscap.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thecommanderscap_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thecommunity.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/theconcoction.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thedanceoflight.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thedarkmaster.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thedevourerofcods.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/theelixir.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/theetomer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/theetomerglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/theevolution.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thefinaldawn.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thefirstshadowflame.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/theformalfootwear.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/theformalfootwear_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thegift.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thegodsgambit.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thegrandgarment.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thegrandgarment_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thegrandgarment_waist.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thehive.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thehive_glow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thehousingcontract.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thejailor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thelastmourning.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/themaelstrom.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/themicrowave.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/themonument.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/themutilator.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/theobliterator.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/theoldreaper.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/theoracle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thepack.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thepact.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thepointer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thepointer_active.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/theprince.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thermaltorch.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thermaltorch_flame.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thermoclineblaster.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thesandwich.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thesevensstriker.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thesponge.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thespongereal.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thespongeshield.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thestorm.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/theswarmer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thesyringe.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thetransformer.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thewand.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thiefsdime.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/thornblossom.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/threadoferadication.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/threadoferadicationglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/throwingbrick.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/timebolt.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tiredtail.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tiredtailpallette.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tiredtailsegment.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tiredtailtail.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tiredtail_wings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/titanarm.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/titanheart.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/titanheartboots.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/titanheartboots_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/titanheartmantle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/titanheartmantle_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/titanheartmask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/titanheartmask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/titaniumrailgun.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/titaniumshuriken.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/toastybatbottle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/toothball.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/topazcrawlerbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/torrentialtear.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/totalitybreakers.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/toxibow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/toxicanttwister.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/toxicatfishbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/toxicheart.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/toxicminnowbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tradewinds.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tranquilitycandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tranquilitycandle_flame.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/trasherbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/trashmantrashcan.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/triactistruepaladinianmagehammerofmight.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/trilobitebanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/trinketofchi.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/truearkoftheancients.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/truearkoftheancientsglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/truebiomeblade.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/truecausticedge.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/trustyoldrod.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tumbleweed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tundraflameblossomsstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tundraleash.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/turbulance.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/twinklerinabottle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/twinkleritem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/twinklingpollox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/twistingnether.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/twistingthunder.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/typhonsgreed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/tyrannysend.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/uelibloombar.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/uelibloombrick.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/uelibloombrickwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/uelibloomore.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ultima.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ultimuscleaver.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ultraliquidator.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/umbraphileboots.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/umbraphileboots_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/umbraphilehood.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/umbraphilehood_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/umbraphileregalia.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/umbraphileregalia_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/unbreakablevoucher.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/undinesretribution.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/unholycore.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/unholyessence.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/unholytonic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/universalgenesis.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/universesplitter.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/unsafenavystonewall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/unstablecastersgauntlet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/unstablecastersgauntlet_handson.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/unstablegranitecore.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/urchinmace.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/urchinstinger.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/ursasergeant.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/utensilpoker.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/v8000engine.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/v8engine.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/valediction.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/valkyrieray.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/vampirictalisman.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/vanishingpoint.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/vanquisherarrow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/vanquisherarrowglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/veeringwind.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/vega.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/vegaglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/vehemence.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/veinburster.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/veneratedlocket.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/vengefulsunstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/venusiantrident.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/veriumbolt.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/vernalbolter.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/vernalbolterglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/vernalsoil.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/verstaltitefishingrod.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/vesuvius.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/vesuviusglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/vicioustonic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/victidebreastplate.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/victidebreastplate_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/victidebreastplate_bulk.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/victidefaulds_waist.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/victidegreaves.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/victidegreaves_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/victideheadmagic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/victideheadmagic_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/victideheadmelee.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/victideheadmelee_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/victideheadranged.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/victideheadranged_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/victideheadrogue.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/victideheadrogue_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/victideheadsummon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/victideheadsummon_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/vigilance.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/vigorouscandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/vilefeeder.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/violence.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/viperfishbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/viralsprout.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/viridvanguard.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/virulence.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/virulingbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/viscera.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/vitaljelly.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/vitriolicviper.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/vividclarity.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/vividclarityglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/vodka.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidbathtub.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidbed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidbookcase.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidcandelabra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidcandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidchair.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidchandelier.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidchest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidconcentrationstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidcondenser.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voiddoor.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voiddresser.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voideatermarionette.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidedge.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidlamp.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidlantern.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidobelisk.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidofcalamity.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidpiano.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidragon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidsink.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidsofa.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidstone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidstoneslab.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidstoneslabwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidstonewall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidstriders.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidstriders_wings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidtable.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidtorch.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidtorch_flame.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidvortex.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voidworkbench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/volcanicsand.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voltageregulationsystem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voltaicclimax.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voltaicjelly.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/volterion.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/voodoodemonvoodoodoll.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/vortexpopper.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/vulcan.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/vulcanglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/vulcanitelance.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/walkingcane.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/warbanneroftherighteous.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/warbanneroftherighteous_balloon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/warloksmoonfist.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/waterturret.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wavepounder.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/waveskipper.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/waywasher.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/weavertrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/webball.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/weightlesscandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/whiskey.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/whitepearlpile.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/whiteseekingmechanism.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/whitewater.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/whitewine.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wildfirebloom.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wildfirebloomglow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/willowisp.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/willowisp2.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/windblade.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wingman.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wingmanalt.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wingsofrebirth.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wingsofrebirth_wings.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wingsofrebirth_wings_real.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wingtimehairdye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wintersfury.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/witherblossomsstaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wrathwing.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumacrobaticspack.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumacrobaticspack_back.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumamplifierbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumbathtub.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumbattery.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumbed.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumblunderbuss.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumbookcase.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumcandelabra.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumcandle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumchair.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumchandelier.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumchest.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumclock.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumcomplexpanelwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumcontroller.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumdiggingturtle.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumdresser.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumdrill.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumdronebanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumdye.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumenergybarrier.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumenergybarrierwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumfusioncannon.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumglobe.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumgyratorbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumhat.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumhat_femalehead.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumhat_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumhat_headextension.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumhovercraftbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumjacket.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumjacket_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumknife.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumlabstationitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumlamp.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumlantern.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumlureitem.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrummetalscrap.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrummetalscrap2.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumoveralls.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumoveralls_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumpanels.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumpanelwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumpiano.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumpillar.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumplatform.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumplatform_glow.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumplating.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumplatingwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumprosthesis.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumprosthesis_arm.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumrod.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumroverbanner.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumscaffoldkit.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumscrewdriver.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumsheets.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumsheetwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumsiding.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumsidingwall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumsink.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumsofa.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumtable.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumtoilet.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumtreasurepinger.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumwallmountedbulb.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wulfrumworkbench.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/wyvernscall.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/xyk2_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/xyk2_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/xyk2_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/xyksblessingblue.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/xyksblessingblueanim.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/xyksblessingblueanim2.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/xyksblessingorange.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/xyksblessingorangeanim.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/xyksblessingorangeanim2.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/xyk_body.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/xyk_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/xyk_legs.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/yateveobloom.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/yellowcoral.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/yellowseekingmechanism.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/yharimscrystal.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/yharimsgift.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/yharonbag.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/yharonegg.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/yharonlegacymusicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/yharonmask.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/yharonmask_head.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/yharonphase1musicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/yharonphase2musicbox.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/yharonrelic.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/yharonskindlestaff.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/yharonsoulfragment.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/yharontrophy.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/yinyo.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/zeniththrone.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/zenpotion.png")]
[assembly: AssemblyAssociatedContentFile("assets/calamity/icons/zergpotion.png")]
[assembly: AssemblyAssociatedContentFile("assets/npc_icons/107.png")]
[assembly: AssemblyAssociatedContentFile("assets/npc_icons/108.png")]
[assembly: AssemblyAssociatedContentFile("assets/npc_icons/124.png")]
[assembly: AssemblyAssociatedContentFile("assets/npc_icons/142.png")]
[assembly: AssemblyAssociatedContentFile("assets/npc_icons/160.png")]
[assembly: AssemblyAssociatedContentFile("assets/npc_icons/17.png")]
[assembly: AssemblyAssociatedContentFile("assets/npc_icons/178.png")]
[assembly: AssemblyAssociatedContentFile("assets/npc_icons/18.png")]
[assembly: AssemblyAssociatedContentFile("assets/npc_icons/19.png")]
[assembly: AssemblyAssociatedContentFile("assets/npc_icons/20.png")]
[assembly: AssemblyAssociatedContentFile("assets/npc_icons/207.png")]
[assembly: AssemblyAssociatedContentFile("assets/npc_icons/208.png")]
[assembly: AssemblyAssociatedContentFile("assets/npc_icons/209.png")]
[assembly: AssemblyAssociatedContentFile("assets/npc_icons/22.png")]
[assembly: AssemblyAssociatedContentFile("assets/npc_icons/227.png")]
[assembly: AssemblyAssociatedContentFile("assets/npc_icons/228.png")]
[assembly: AssemblyAssociatedContentFile("assets/npc_icons/229.png")]
[assembly: AssemblyAssociatedContentFile("assets/npc_icons/353.png")]
[assembly: AssemblyAssociatedContentFile("assets/npc_icons/369.png")]
[assembly: AssemblyAssociatedContentFile("assets/npc_icons/37.png")]
[assembly: AssemblyAssociatedContentFile("assets/npc_icons/38.png")]
[assembly: AssemblyAssociatedContentFile("assets/npc_icons/453.png")]
[assembly: AssemblyAssociatedContentFile("assets/npc_icons/54.png")]
[assembly: AssemblyAssociatedContentFile("assets/npc_icons/550.png")]
[assembly: AssemblyAssociatedContentFile("assets/npc_icons/588.png")]
[assembly: AssemblyAssociatedContentFile("assets/npc_icons/633.png")]
[assembly: AssemblyAssociatedContentFile("assets/npc_icons/663.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/body/armhand.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/body/armshirt.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/body/armskin.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/body/armundershirt.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/body/eyes.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/body/eyewhites.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/body/hands.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/body/head.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/body/legskin.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/body/pants.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/body/shirt.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/body/shoes.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/body/torsoskin.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/body/undershirt.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/1.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/10.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/100.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/101.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/102.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/103.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/104.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/105.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/106.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/107.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/108.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/109.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/11.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/110.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/111.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/112.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/113.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/114.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/115.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/116.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/117.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/118.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/119.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/12.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/120.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/121.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/122.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/123.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/124.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/125.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/126.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/127.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/128.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/129.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/13.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/130.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/131.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/132.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/133.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/134.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/135.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/136.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/137.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/138.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/139.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/14.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/140.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/141.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/142.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/143.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/144.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/145.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/146.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/147.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/148.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/149.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/15.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/150.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/151.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/152.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/153.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/154.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/155.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/156.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/157.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/158.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/159.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/16.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/160.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/161.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/162.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/163.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/164.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/165.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/166.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/167.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/168.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/169.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/17.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/170.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/171.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/172.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/173.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/174.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/175.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/176.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/177.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/178.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/179.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/18.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/180.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/181.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/182.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/183.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/184.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/185.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/186.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/187.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/188.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/189.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/19.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/190.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/191.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/192.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/193.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/194.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/195.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/196.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/197.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/198.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/199.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/2.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/20.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/200.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/201.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/202.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/203.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/204.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/205.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/206.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/207.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/208.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/209.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/21.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/210.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/211.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/212.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/213.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/214.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/215.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/216.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/217.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/218.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/219.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/22.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/220.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/221.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/222.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/223.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/224.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/225.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/226.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/227.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/228.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/23.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/24.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/25.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/26.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/27.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/28.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/29.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/3.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/30.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/31.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/32.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/33.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/34.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/35.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/36.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/37.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/38.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/39.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/4.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/40.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/41.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/42.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/43.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/44.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/45.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/46.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/47.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/48.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/49.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/5.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/50.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/51.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/52.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/53.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/54.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/55.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/56.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/57.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/58.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/59.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/6.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/60.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/61.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/62.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/63.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/64.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/65.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/66.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/67.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/68.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/69.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/7.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/70.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/71.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/72.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/73.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/74.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/75.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/76.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/77.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/78.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/79.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/8.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/80.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/81.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/82.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/83.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/84.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/85.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/86.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/87.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/88.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/89.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/9.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/90.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/91.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/92.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/93.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/94.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/95.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/96.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/97.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/98.png")]
[assembly: AssemblyAssociatedContentFile("assets/player/hair/99.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/1.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/10.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/100.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/101.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/102.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/103.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/104.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/105.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/106.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/107.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/108.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/109.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/11.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/110.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/111.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/112.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/113.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/114.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/115.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/116.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/117.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/118.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/119.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/12.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/120.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/121.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/122.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/123.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/124.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/125.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/126.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/127.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/128.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/129.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/13.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/130.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/131.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/132.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/133.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/134.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/135.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/136.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/137.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/138.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/139.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/14.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/140.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/141.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/142.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/143.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/144.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/145.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/146.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/147.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/148.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/149.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/15.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/150.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/151.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/152.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/153.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/154.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/155.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/156.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/157.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/158.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/159.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/16.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/160.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/161.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/162.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/163.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/164.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/165.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/166.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/167.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/168.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/169.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/17.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/170.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/171.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/172.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/173.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/174.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/175.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/176.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/177.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/178.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/179.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/18.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/180.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/181.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/182.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/183.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/184.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/185.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/186.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/187.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/188.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/189.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/19.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/190.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/191.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/192.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/193.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/194.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/195.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/196.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/197.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/198.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/199.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/2.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/20.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/200.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/201.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/202.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/203.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/204.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/205.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/206.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/207.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/208.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/209.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/21.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/210.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/211.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/212.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/213.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/214.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/215.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/216.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/217.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/218.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/219.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/22.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/220.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/221.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/222.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/223.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/224.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/225.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/226.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/227.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/228.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/229.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/23.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/230.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/231.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/232.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/233.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/234.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/235.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/236.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/237.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/238.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/239.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/24.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/240.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/241.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/242.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/243.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/244.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/245.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/246.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/247.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/248.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/249.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/25.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/250.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/251.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/252.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/253.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/254.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/255.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/256.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/257.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/258.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/259.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/26.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/260.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/261.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/262.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/263.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/264.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/265.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/266.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/267.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/268.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/269.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/27.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/270.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/271.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/272.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/273.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/274.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/275.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/276.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/277.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/278.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/279.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/28.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/280.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/281.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/282.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/283.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/284.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/285.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/286.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/287.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/288.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/289.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/29.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/290.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/291.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/292.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/293.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/294.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/295.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/296.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/297.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/298.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/299.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/3.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/30.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/300.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/301.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/302.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/303.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/304.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/305.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/306.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/307.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/308.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/309.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/31.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/310.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/311.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/312.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/313.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/314.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/315.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/316.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/317.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/318.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/319.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/32.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/320.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/321.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/322.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/323.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/324.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/325.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/326.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/327.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/328.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/329.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/33.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/330.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/331.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/332.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/333.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/334.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/335.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/336.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/337.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/338.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/339.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/34.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/340.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/341.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/342.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/343.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/344.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/345.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/346.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/347.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/348.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/349.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/35.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/350.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/351.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/352.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/353.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/354.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/36.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/37.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/38.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/39.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/4.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/40.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/41.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/42.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/43.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/44.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/45.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/46.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/47.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/48.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/49.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/5.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/50.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/51.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/52.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/53.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/54.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/55.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/56.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/57.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/58.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/59.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/6.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/60.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/61.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/62.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/63.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/64.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/65.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/66.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/67.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/68.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/69.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/7.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/70.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/71.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/72.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/73.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/74.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/75.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/76.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/77.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/78.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/79.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/8.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/80.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/81.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/82.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/83.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/84.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/85.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/86.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/87.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/88.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/89.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/9.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/90.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/91.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/92.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/93.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/94.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/95.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/96.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/97.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/98.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/buff_icons/99.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/10.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/100.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1000.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1001.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1002.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1003.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1004.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1005.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1006.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1007.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1008.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1009.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/101.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1010.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1011.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1012.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1013.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1014.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1015.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1016.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1017.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1018.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1019.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/102.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1020.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1021.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1022.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1023.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1024.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1025.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1026.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1027.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1028.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1029.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/103.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1030.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1031.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1032.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1033.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1034.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1035.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1036.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1037.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1038.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1039.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/104.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1040.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1041.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1042.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1043.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1044.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1045.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1046.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1047.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1048.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1049.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/105.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1050.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1051.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1052.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1053.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1054.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1055.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1056.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1057.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1058.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1059.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/106.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1060.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1061.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1062.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1063.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1064.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1065.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1066.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1067.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1068.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1069.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/107.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1070.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1071.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1072.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1073.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1074.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1075.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1076.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1077.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1078.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1079.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/108.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1080.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1081.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1082.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1083.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1084.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1085.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1086.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1087.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1088.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1089.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/109.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1090.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1091.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1092.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1093.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1094.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1095.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1096.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1097.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1098.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1099.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/11.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/110.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1100.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1101.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1102.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1103.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1104.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1105.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1106.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1107.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1108.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1109.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/111.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1110.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1111.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1112.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1113.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1114.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1115.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1116.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1117.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1118.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1119.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/112.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1120.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1121.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1122.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1123.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1124.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1125.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1126.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1127.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1128.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1129.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/113.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1130.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1131.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1132.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1133.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1134.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1135.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1136.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1137.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1138.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1139.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/114.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1140.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1141.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1142.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1143.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1144.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1145.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1146.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1147.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1148.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1149.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/115.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1150.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1151.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1152.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1153.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1154.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1155.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1156.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1157.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1158.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1159.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/116.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1160.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1161.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1162.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1163.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1164.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1165.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1166.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1167.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1168.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1169.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/117.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1170.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1171.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1172.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1173.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1174.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1175.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1176.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1177.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1178.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1179.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/118.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1180.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1181.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1182.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1183.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1184.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1185.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1186.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1187.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1188.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/119.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1190.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1191.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1192.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1193.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1194.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1195.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1196.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1197.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1198.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1199.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/12.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/120.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1200.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1201.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1202.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1203.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1204.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1205.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1206.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1207.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1208.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1209.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/121.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1210.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1211.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1212.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1213.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1214.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1215.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1216.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1217.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1218.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1219.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/122.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1220.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1221.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1222.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1223.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1224.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1225.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1226.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1227.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1228.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1229.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/123.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1230.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1231.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1232.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1233.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1234.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1235.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1236.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1237.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1238.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1239.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/124.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1240.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1241.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1242.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1243.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1244.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1245.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1246.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1247.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1248.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1249.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/125.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1250.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1251.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1252.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1253.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1254.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1255.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1256.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1257.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1258.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1259.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/126.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1260.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1261.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1262.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1263.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1264.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1265.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1266.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1267.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1268.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1269.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/127.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1270.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1271.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1272.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1273.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1274.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1275.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1276.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1277.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1278.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1279.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/128.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1280.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1281.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1282.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1283.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1284.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1285.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1286.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1287.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1288.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1289.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/129.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1290.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1291.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1292.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1293.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1294.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1295.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1296.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1297.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1298.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1299.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/13.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/130.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1300.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1301.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1302.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1303.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1304.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1305.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1306.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1307.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1308.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1309.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/131.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1310.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1311.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1312.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1313.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1314.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1315.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1316.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1317.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1318.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1319.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/132.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1320.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1321.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1322.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1323.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1324.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1325.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1326.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1327.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1328.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1329.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/133.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1330.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1331.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1332.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1333.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1334.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1335.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1336.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1337.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1338.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1339.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/134.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1340.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1341.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1342.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1343.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1344.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1345.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1346.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1347.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1348.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1349.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/135.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1350.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1351.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1352.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1353.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1354.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1355.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1356.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1357.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1358.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1359.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/136.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1360.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1361.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1362.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1363.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1364.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1365.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1366.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1367.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1368.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1369.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/137.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1370.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1371.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1372.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1373.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1374.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1375.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1376.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1377.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1378.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1379.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/138.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1380.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1381.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1382.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1383.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1384.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1385.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1386.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1387.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1388.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1389.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/139.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1390.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1391.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1392.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1393.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1394.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1395.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1396.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1397.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1398.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1399.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/14.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/140.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1400.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1401.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1402.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1403.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1404.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1405.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1406.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1407.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1408.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1409.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/141.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1410.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1411.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1412.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1413.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1414.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1415.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1416.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1417.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1418.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1419.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/142.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1420.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1421.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1422.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1423.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1424.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1425.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1426.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1427.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1428.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1429.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/143.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1430.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1431.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1432.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1433.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1434.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1435.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1436.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1437.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1438.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1439.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/144.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1440.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1441.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1442.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1443.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1444.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1445.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1446.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1447.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1448.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1449.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/145.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1450.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1451.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1452.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1453.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1454.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1455.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1456.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1457.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1458.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1459.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/146.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1460.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1461.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1462.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1463.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1464.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1465.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1466.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1467.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1468.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1469.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/147.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1470.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1471.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1472.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1473.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1474.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1475.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1476.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1477.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1478.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1479.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/148.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1480.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1481.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1482.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1483.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1484.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1485.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1486.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1487.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1488.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1489.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/149.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1490.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1491.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1492.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1493.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1494.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1495.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1496.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1497.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1498.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1499.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/15.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/150.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1500.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1501.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1502.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1503.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1504.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1505.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1506.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1507.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1508.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1509.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/151.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1510.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1511.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1512.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1513.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1514.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1515.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1516.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1517.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1518.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1519.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/152.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1520.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1521.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1522.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1523.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1524.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1525.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1526.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1527.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1528.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1529.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/153.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1530.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1531.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1532.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1533.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1534.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1535.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1536.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1537.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1538.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1539.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/154.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1540.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1541.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1542.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1543.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1544.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1545.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1546.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1547.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1548.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1549.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/155.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1550.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1551.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1552.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1553.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1554.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1555.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1556.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1557.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1558.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1559.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/156.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1560.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1561.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1562.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1563.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1564.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1565.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1566.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1567.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1568.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1569.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/157.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1570.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1571.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1572.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1573.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1574.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1575.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1576.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1577.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1578.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1579.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/158.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1580.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1581.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1582.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1583.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1584.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1585.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1586.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1587.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1588.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1589.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/159.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1590.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1591.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1592.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1593.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1594.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1595.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1596.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1597.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1598.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1599.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/16.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/160.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1600.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1601.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1602.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1603.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1604.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1605.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1606.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1607.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1608.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1609.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/161.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1610.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1611.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1612.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1613.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1614.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1615.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1616.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1617.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1618.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1619.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/162.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1620.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1621.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1622.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1623.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1624.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1625.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1626.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1627.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1628.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1629.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/163.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1630.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1631.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1632.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1633.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1634.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1635.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1636.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1637.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1638.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1639.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/164.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1640.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1641.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1642.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1643.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1644.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1645.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1646.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1647.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1648.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1649.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/165.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1650.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1651.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1652.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1653.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1654.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1655.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1656.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1657.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1658.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1659.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/166.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1660.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1661.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1662.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1663.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1664.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1665.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1666.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1667.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1668.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1669.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/167.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1670.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1671.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1672.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1673.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1674.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1675.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1676.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1677.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1678.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1679.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/168.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1680.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1681.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1682.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1683.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1684.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1685.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1686.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1687.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1688.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1689.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/169.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1690.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1691.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1692.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1693.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1694.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1695.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1696.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1697.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1698.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1699.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/17.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/170.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1700.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1701.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1702.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1703.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1704.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1705.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1706.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1707.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1708.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1709.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/171.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1710.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1711.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1712.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1713.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1714.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1715.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1716.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1717.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1718.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1719.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/172.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1720.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1721.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1722.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1723.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1724.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1725.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1726.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1727.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1728.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1729.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/173.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1730.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1731.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1732.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1733.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1734.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1735.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1736.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1737.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1738.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1739.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/174.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1740.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1741.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1742.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1743.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1744.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1745.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1746.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1747.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1748.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1749.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/175.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1750.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1751.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1752.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1753.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1754.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1755.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1756.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1757.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1758.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1759.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/176.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1760.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1761.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1762.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1763.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1764.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1765.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1766.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1767.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1768.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1769.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/177.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1770.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1771.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1772.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1773.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1774.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1775.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1776.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1777.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1778.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1779.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/178.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1780.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1781.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1782.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1783.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1784.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1785.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1786.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1787.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1788.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1789.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/179.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1790.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1791.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1792.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1793.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1794.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1795.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1796.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1797.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1798.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1799.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/18.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/180.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1800.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1801.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1802.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1803.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1804.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1805.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1806.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1807.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1808.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1809.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/181.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1810.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1811.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1812.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1813.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1814.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1815.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1816.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1817.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1818.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1819.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/182.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1820.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1821.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1822.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1823.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1824.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1825.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1826.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1827.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1828.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1829.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/183.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1830.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1831.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1832.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1833.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1834.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1835.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1836.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1837.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1838.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1839.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/184.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1840.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1841.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1842.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1843.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1844.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1845.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1846.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1847.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1848.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1849.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/185.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1850.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1851.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1852.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1853.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1854.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1855.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1856.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1857.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1858.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1859.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/186.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1860.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1861.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1862.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1863.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1864.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1865.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1866.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1867.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1868.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1869.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/187.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1870.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1871.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1872.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1873.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1874.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1875.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1876.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1877.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1878.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1879.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/188.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1880.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1881.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1882.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1883.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1884.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1885.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1886.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1887.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1888.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1889.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/189.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1890.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1891.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1892.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1893.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1894.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1895.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1896.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1897.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1898.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1899.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/19.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/190.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1900.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1901.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1902.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1903.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1904.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1905.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1906.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1907.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1908.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1909.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/191.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1910.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1911.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1912.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1913.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1914.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1915.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1916.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1917.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1918.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1919.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/192.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1920.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1921.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1922.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1923.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1924.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1925.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1926.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1927.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1928.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1929.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/193.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1930.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1931.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1932.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1933.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1934.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1935.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1936.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1937.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1938.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1939.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/194.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1940.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1941.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1942.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1943.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1944.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1945.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1946.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1947.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1948.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1949.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/195.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1950.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1951.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1952.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1953.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1954.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1955.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1956.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1957.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1958.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1959.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/196.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1960.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1961.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1962.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1963.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1964.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1965.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1966.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1967.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1968.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1969.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/197.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1970.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1971.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1972.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1973.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1974.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1975.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1976.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1977.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1978.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1979.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/198.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1980.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1981.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1982.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1983.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1984.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1985.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1986.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1987.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1988.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1989.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/199.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1990.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1991.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1992.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1993.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1994.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1995.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1996.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1997.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1998.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/1999.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/20.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/200.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2000.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2001.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2002.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2003.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2004.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2005.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2006.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2007.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2008.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2009.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/201.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2010.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2011.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2012.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2013.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2014.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2015.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2016.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2017.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2018.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2019.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/202.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2020.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2021.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2022.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2023.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2024.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2025.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2026.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2027.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2028.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2029.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/203.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2030.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2031.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2032.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2033.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2034.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2035.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2036.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2037.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2038.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2039.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/204.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2040.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2041.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2042.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2043.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2044.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2045.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2046.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2047.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2048.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2049.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/205.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2050.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2051.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2052.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2053.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2054.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2055.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2056.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2057.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2058.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2059.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/206.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2060.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2061.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2062.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2063.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2064.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2065.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2066.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2067.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2068.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2069.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/207.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2070.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2071.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2072.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2073.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2074.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2075.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2076.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2077.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2078.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2079.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/208.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2080.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2081.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2082.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2083.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2084.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2085.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2086.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2087.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2088.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2089.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/209.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2090.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2091.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2092.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2093.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2094.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2095.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2096.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2097.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2098.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2099.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/21.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/210.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2100.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2101.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2102.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2103.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2104.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2105.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2106.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2107.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2108.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2109.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/211.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2110.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2111.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2112.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2113.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2114.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2115.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2116.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2117.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2118.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2119.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/212.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2120.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2121.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2122.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2123.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2124.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2125.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2126.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2127.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2128.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2129.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/213.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2130.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2131.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2132.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2133.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2134.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2135.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2136.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2137.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2138.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2139.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/214.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2140.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2141.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2142.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2143.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2144.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2145.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2146.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2147.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2148.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2149.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/215.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2150.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2151.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2152.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2153.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2154.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2155.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2156.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2157.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2158.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2159.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/216.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2160.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2161.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2162.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2163.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2164.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2165.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2166.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2167.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2168.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2169.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/217.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2170.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2171.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2172.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2173.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2174.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2175.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2176.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2177.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2178.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2179.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/218.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2180.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2181.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2182.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2183.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2184.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2185.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2186.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2187.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2188.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2189.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/219.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2190.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2191.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2192.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2193.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2194.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2195.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2196.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2197.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2198.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2199.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/22.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/220.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2200.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2201.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2202.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2203.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2204.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2205.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2206.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2207.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2208.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2209.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/221.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2210.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2211.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2212.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2213.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2214.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2215.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2216.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2217.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2218.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2219.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/222.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2220.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2221.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2222.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2223.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2224.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2225.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2226.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2227.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2228.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2229.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/223.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2230.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2231.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2232.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2233.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2234.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2235.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2236.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2237.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2238.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2239.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/224.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2240.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2241.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2242.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2243.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2244.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2245.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2246.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2247.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2248.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2249.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/225.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2250.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2251.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2252.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2253.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2254.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2255.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2256.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2257.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2258.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2259.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/226.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2260.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2261.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2262.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2263.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2264.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2265.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2266.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2267.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2268.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2269.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/227.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2270.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2271.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2272.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2273.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2274.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2275.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2276.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2277.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2278.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2279.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/228.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2280.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2281.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2282.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2283.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2284.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2285.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2286.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2287.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2288.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2289.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/229.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2290.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2291.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2292.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2293.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2294.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2295.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2296.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2297.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2298.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2299.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/23.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/230.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2300.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2301.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2302.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2303.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2304.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2305.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2306.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2307.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2308.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2309.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/231.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2310.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2311.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2312.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2313.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2314.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2315.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2316.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2317.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2318.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2319.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/232.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2320.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2321.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2322.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2323.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2324.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2325.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2326.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2327.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2328.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2329.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/233.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2330.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2331.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2332.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2333.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2334.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2335.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2336.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2337.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2338.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2339.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/234.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2340.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2341.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2342.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2343.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2344.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2345.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2346.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2347.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2348.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2349.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/235.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2350.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2351.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2352.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2353.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2354.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2355.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2356.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2357.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2358.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2359.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/236.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2360.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2361.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2362.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2363.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2364.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2365.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2366.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2367.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2368.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2369.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/237.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2370.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2371.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2372.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2373.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2374.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2375.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2376.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2377.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2378.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2379.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/238.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2380.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2381.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2382.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2383.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2384.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2385.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2386.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2387.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2388.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2389.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/239.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2390.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2391.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2392.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2393.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2394.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2395.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2396.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2397.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2398.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2399.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/24.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/240.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2400.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2401.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2402.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2403.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2404.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2405.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2406.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2407.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2408.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2409.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/241.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2410.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2411.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2412.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2413.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2414.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2415.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2416.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2417.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2418.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2419.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/242.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2420.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2421.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2422.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2423.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2424.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2425.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2426.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2427.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2428.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2429.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/243.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2430.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2431.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2432.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2433.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2434.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2435.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2436.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2437.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2438.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2439.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/244.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2440.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2441.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2442.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2443.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2444.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2445.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2446.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2447.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2448.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2449.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/245.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2450.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2451.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2452.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2453.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2454.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2455.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2456.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2457.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2458.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2459.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/246.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2460.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2461.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2462.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2463.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2464.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2465.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2466.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2467.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2468.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2469.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/247.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2470.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2471.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2472.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2473.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2474.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2475.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2476.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2477.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2478.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2479.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/248.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2480.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2481.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2482.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2483.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2484.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2485.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2486.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2487.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2488.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2489.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/249.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2490.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2491.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2492.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2493.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2494.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2495.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2496.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2497.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2498.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2499.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/25.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/250.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2500.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2501.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2502.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2503.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2504.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2505.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2506.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2507.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2508.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2509.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/251.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2510.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2511.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2512.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2513.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2514.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2515.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2516.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2517.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2518.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2519.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/252.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2520.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2521.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2522.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2523.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2524.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2525.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2526.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2527.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2528.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2529.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/253.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2530.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2531.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2532.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2533.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2534.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2535.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2536.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2537.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2538.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2539.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/254.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2540.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2541.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2542.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2543.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2544.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2545.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2546.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2547.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2548.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2549.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/255.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2550.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2551.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2552.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2553.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2554.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2555.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2556.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2557.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2558.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2559.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/256.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2560.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2561.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2562.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2563.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2564.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2565.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2566.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2567.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2568.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2569.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/257.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2570.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2571.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2572.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2573.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2574.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2575.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2576.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2577.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2578.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2579.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/258.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2580.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2581.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2582.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2583.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2584.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2585.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2586.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2587.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2588.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2589.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/259.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2590.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2591.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2592.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2593.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2594.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2595.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2596.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2597.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2598.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2599.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/26.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/260.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2600.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2601.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2602.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2603.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2604.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2605.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2606.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2607.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2608.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2609.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/261.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2610.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2611.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2612.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2613.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2614.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2615.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2616.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2617.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2618.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2619.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/262.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2620.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2621.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2622.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2623.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2624.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2625.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2626.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2627.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2628.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2629.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/263.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2630.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2631.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2632.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2633.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2634.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2635.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2636.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2637.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2638.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2639.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/264.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2640.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2641.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2642.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2643.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2644.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2645.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2646.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2647.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2648.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2649.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/265.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2650.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2651.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2652.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2653.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2654.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2655.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2656.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2657.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2658.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2659.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/266.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2660.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2661.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2662.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2663.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2664.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2665.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2666.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2667.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2668.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2669.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/267.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2670.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2671.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2672.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2673.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2674.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2675.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2676.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2677.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2678.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2679.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/268.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2680.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2681.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2682.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2683.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2684.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2685.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2686.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2687.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2688.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2689.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/269.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2690.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2691.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2692.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2693.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2694.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2695.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2696.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2697.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2698.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2699.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/27.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/270.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2700.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2701.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2702.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2703.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2704.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2705.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2706.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2707.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2708.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2709.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/271.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2710.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2711.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2712.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2713.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2714.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2715.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2716.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2717.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2718.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2719.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/272.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2720.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2721.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2722.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2723.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2724.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2725.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2726.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2727.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2728.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2729.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/273.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2730.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2731.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2732.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2733.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2734.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2735.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2736.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2737.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2738.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2739.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/274.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2740.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2741.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2742.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2743.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2744.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2745.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2746.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2747.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2748.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2749.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/275.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2750.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2751.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2752.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2753.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2754.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2755.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2756.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2757.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2758.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2759.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/276.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2760.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2761.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2762.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2763.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2764.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2765.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2766.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2767.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2768.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2769.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/277.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2770.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2771.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2772.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2773.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2774.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2775.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2776.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2777.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2778.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2779.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/278.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2780.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2781.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2782.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2783.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2784.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2785.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2786.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2787.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2788.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2789.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/279.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2790.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2791.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2792.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2793.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2794.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2795.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2796.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2797.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2798.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2799.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/28.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/280.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2800.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2801.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2802.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2803.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2804.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2805.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2806.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2807.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2808.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2809.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/281.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2810.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2811.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2812.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2813.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2814.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2815.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2816.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2817.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2818.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2819.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/282.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2820.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2821.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2822.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2823.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2824.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2825.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2826.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2827.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2828.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2829.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/283.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2830.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2831.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2832.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2833.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2834.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2835.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2836.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2837.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2838.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2839.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/284.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2840.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2841.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2842.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2843.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2844.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2845.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2846.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2847.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2848.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2849.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/285.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2850.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2851.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2852.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2853.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2854.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2855.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2856.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2857.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2858.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2859.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/286.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2860.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2861.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2862.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2863.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2864.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2865.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2866.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2867.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2868.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2869.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/287.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2870.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2871.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2872.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2873.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2874.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2875.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2876.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2877.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2878.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2879.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/288.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2880.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2881.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2882.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2883.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2884.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2885.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2886.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2887.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2888.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2889.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/289.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2890.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2891.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2892.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2893.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2894.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2895.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2896.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2897.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2898.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2899.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/29.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/290.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2900.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2901.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2902.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2903.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2904.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2905.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2906.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2907.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2908.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2909.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/291.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2910.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2911.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2912.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2913.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2914.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2915.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2916.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2917.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2918.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2919.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/292.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2920.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2921.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2922.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2923.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2924.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2925.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2926.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2927.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2928.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2929.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/293.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2930.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2931.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2932.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2933.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2934.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2935.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2936.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2937.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2938.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2939.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/294.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2940.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2941.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2942.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2943.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2944.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2945.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2946.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2947.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2948.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2949.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/295.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2950.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2951.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2952.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2953.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2954.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2955.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2956.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2957.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2958.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2959.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/296.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2960.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2961.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2962.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2963.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2964.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2965.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2966.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2967.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2968.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2969.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/297.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2970.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2971.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2972.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2973.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2974.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2975.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2976.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2977.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2978.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2979.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/298.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2980.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2981.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2982.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2983.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2984.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2985.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2986.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2987.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2988.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2989.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/299.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2990.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2991.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2992.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2993.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2994.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2995.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2996.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2997.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2998.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/2999.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/30.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/300.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3000.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3001.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3002.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3003.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3004.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3005.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3006.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3007.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3008.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3009.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/301.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3010.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3011.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3012.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3013.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3014.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3015.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3016.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3017.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3018.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3019.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/302.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3020.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3021.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3022.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3023.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3024.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3025.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3026.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3027.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3028.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3029.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/303.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3030.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3031.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3032.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3033.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3034.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3035.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3036.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3037.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3038.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3039.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/304.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3040.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3041.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3042.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3043.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3044.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3045.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3046.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3047.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3048.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3049.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/305.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3050.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3051.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3052.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3053.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3054.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3055.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3056.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3057.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3058.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3059.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/306.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3060.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3061.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3062.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3063.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3064.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3065.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3066.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3067.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3068.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3069.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/307.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3070.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3071.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3072.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3073.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3074.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3075.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3076.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3077.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3078.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3079.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/308.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3080.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3081.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3082.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3083.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3084.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3085.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3086.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3087.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3088.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3089.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/309.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3090.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3091.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3092.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3093.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3094.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3095.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3096.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3097.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3098.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3099.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/31.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/310.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3100.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3101.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3102.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3103.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3104.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3105.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3106.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3107.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3108.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3109.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/311.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3110.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3111.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3112.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3113.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3114.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3115.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3116.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3117.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3118.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3119.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/312.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3120.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3121.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3122.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3123.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3124.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3125.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3126.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3127.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3128.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3129.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/313.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3130.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3131.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3132.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3133.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3134.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3135.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3136.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3137.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3138.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3139.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/314.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3140.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3141.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3142.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3143.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3144.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3145.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3146.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3147.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3148.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3149.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/315.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3150.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3151.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3152.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3153.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3154.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3155.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3156.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3157.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3158.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3159.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/316.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3160.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3161.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3162.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3163.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3164.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3165.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3166.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3167.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3168.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3169.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/317.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3170.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3171.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3172.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3173.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3174.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3175.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3176.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3177.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3178.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3179.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/318.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3180.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3181.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3182.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3183.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3184.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3185.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3186.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3187.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3188.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3189.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/319.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3190.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3191.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3192.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3193.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3194.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3195.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3196.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3197.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3198.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3199.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/32.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/320.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3200.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3201.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3202.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3203.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3204.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3205.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3206.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3207.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3208.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3209.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/321.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3210.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3211.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3212.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3213.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3214.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3215.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3216.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3217.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3218.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3219.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/322.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3220.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3221.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3222.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3223.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3224.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3225.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3226.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3227.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3228.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3229.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/323.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3230.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3231.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3232.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3233.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3234.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3235.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3236.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3237.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3238.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3239.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/324.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3240.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3241.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3242.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3243.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3244.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3245.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3246.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3247.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3248.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3249.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/325.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3250.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3251.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3252.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3253.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3254.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3255.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3256.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3257.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3258.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3259.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/326.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3260.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3261.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3262.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3263.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3264.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3265.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3266.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3267.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3268.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3269.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/327.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3270.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3271.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3272.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3273.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3274.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3275.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3276.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3277.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3278.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3279.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/328.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3280.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3281.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3282.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3283.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3284.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3285.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3286.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3287.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3288.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3289.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/329.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3290.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3291.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3292.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3293.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3294.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3295.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3296.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3297.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3298.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3299.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/33.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/330.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3300.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3301.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3302.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3303.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3304.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3305.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3306.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3307.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3308.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3309.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/331.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3310.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3311.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3312.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3313.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3314.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3315.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3316.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3317.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3318.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3319.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/332.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3320.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3321.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3322.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3323.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3324.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3325.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3326.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3327.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3328.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3329.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/333.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3330.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3331.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3332.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3333.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3334.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3335.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3336.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3337.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3338.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3339.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/334.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3340.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3341.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3342.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3343.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3344.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3345.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3346.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3347.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3348.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3349.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/335.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3350.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3351.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3352.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3353.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3354.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3355.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3356.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3357.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3358.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3359.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/336.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3360.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3361.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3362.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3363.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3364.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3365.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3366.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3367.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3368.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3369.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/337.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3370.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3371.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3372.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3373.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3374.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3375.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3376.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3377.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3378.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3379.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/338.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3380.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3381.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3382.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3383.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3384.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3385.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3386.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3387.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3388.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3389.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/339.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3390.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3391.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3392.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3393.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3394.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3395.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3396.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3397.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3398.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3399.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/34.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/340.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3400.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3401.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3402.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3403.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3404.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3405.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3406.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3407.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3408.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3409.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/341.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3410.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3411.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3412.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3413.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3414.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3415.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3416.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3417.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3418.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3419.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/342.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3420.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3421.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3422.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3423.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3424.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3425.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3426.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3427.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3428.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3429.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/343.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3430.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3431.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3432.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3433.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3434.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3435.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3436.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3437.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3438.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3439.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/344.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3440.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3441.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3442.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3443.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3444.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3445.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3446.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3447.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3448.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3449.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/345.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3450.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3451.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3452.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3453.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3454.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3455.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3456.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3457.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3458.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3459.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/346.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3460.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3461.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3462.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3463.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3464.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3465.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3466.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3467.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3468.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3469.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/347.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3470.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3471.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3472.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3473.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3474.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3475.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3476.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3477.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3478.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3479.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/348.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3480.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3481.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3482.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3483.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3484.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3485.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3486.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3487.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3488.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3489.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/349.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3490.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3491.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3492.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3493.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3494.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3495.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3496.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3497.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3498.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3499.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/35.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/350.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3500.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3501.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3502.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3503.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3504.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3505.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3506.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3507.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3508.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3509.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/351.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3510.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3511.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3512.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3513.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3514.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3515.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3516.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3517.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3518.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3519.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/352.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3520.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3521.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3522.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3523.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3524.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3525.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3526.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3527.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3528.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3529.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/353.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3530.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3531.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3532.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3533.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3534.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3535.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3536.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3537.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3538.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3539.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/354.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3540.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3541.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3542.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3543.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3544.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3545.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3546.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3547.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3548.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3549.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/355.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3550.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3551.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3552.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3553.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3554.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3555.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3556.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3557.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3558.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3559.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/356.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3560.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3561.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3562.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3563.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3564.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3565.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3566.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3567.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3568.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3569.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/357.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3570.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3571.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3572.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3573.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3574.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3575.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3576.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3577.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3578.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3579.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/358.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3580.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3581.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3582.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3583.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3584.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3585.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3586.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3587.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3588.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3589.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/359.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3590.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3591.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3592.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3593.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3594.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3595.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3596.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3597.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3598.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3599.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/36.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/360.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3600.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3601.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3602.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3603.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3604.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3605.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3606.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3607.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3608.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3609.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/361.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3610.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3611.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3612.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3613.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3614.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3615.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3616.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3617.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3618.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3619.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/362.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3620.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3621.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3622.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3623.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3624.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3625.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3626.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3627.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3628.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3629.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/363.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3630.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3631.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3632.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3633.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3634.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3635.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3636.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3637.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3638.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3639.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/364.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3640.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3641.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3642.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3643.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3644.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3645.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3646.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3647.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3648.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3649.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/365.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3650.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3651.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3652.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3653.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3654.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3655.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3656.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3657.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3658.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3659.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/366.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3660.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3661.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3662.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3663.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3664.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3665.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3666.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3667.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3668.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3669.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/367.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3670.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3671.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3672.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3673.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3674.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3675.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3676.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3677.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3678.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3679.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/368.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3680.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3681.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3682.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3683.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3684.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3685.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3686.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3687.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3688.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3689.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/369.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3690.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3691.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3692.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3693.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3694.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3695.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3696.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3697.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3698.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3699.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/37.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/370.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3700.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3701.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3702.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3703.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3704.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3705.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3706.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3707.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3708.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3709.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/371.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3710.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3711.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3712.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3713.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3714.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3715.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3716.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3717.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3718.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3719.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/372.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3720.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3721.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3722.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3723.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3724.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3725.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3726.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3727.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3728.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3729.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/373.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3730.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3731.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3732.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3733.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3734.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3735.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3736.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3737.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3738.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3739.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/374.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3740.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3741.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3742.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3743.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3744.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3745.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3746.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3747.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3748.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3749.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/375.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3750.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3751.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3752.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3753.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3754.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3755.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3756.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3757.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3758.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3759.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/376.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3760.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3761.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3762.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3763.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3764.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3765.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3766.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3767.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3768.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3769.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/377.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3770.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3771.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3772.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3773.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3774.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3775.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3776.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3777.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3778.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3779.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/378.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3780.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3781.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3782.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3783.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3784.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3785.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3786.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3787.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3788.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3789.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/379.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3790.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3791.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3792.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3793.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3794.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3795.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3796.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3797.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3798.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3799.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/38.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/380.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3800.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3801.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3802.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3803.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3804.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3805.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3806.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3807.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3808.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3809.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/381.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3810.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3811.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3812.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3813.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3814.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3815.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3816.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3817.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3818.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3819.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/382.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3820.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3821.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3822.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3823.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3824.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3825.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3826.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3827.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3828.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3829.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/383.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3830.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3831.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3832.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3833.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3834.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3835.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3836.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3837.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3838.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3839.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/384.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3840.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3841.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3842.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3843.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3844.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3845.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3846.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3847.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3848.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3849.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/385.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3850.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3851.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3852.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3853.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3854.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3855.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3856.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3857.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3858.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3859.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/386.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3860.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3861.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3862.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3863.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3864.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3865.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3866.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3867.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3868.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3869.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/387.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3870.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3871.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3872.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3873.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3874.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3875.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3876.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3877.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3878.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3879.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/388.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3880.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3881.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3882.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3883.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3884.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3885.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3886.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3887.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3888.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3889.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/389.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3890.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3891.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3892.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3893.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3894.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3895.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3896.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3897.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3898.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3899.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/39.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/390.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3900.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3901.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3902.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3903.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3904.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3905.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3906.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3907.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3908.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3909.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/391.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3910.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3911.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3912.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3913.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3914.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3915.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3916.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3917.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3918.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3919.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/392.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3920.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3921.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3922.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3923.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3924.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3925.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3926.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3927.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3928.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3929.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/393.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3930.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3931.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3932.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3933.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3934.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3935.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3936.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3937.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3938.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3939.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/394.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3940.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3941.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3942.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3943.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3944.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3945.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3946.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3947.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3948.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3949.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/395.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3950.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3951.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3952.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3953.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3954.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3955.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3956.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3957.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3958.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3959.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/396.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3960.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3961.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3962.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3963.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3964.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3965.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3966.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3967.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3968.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3969.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/397.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3970.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3971.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3972.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3973.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3974.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3975.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3976.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3977.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3978.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3979.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/398.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3980.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3981.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3982.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3983.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3984.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3985.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3986.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3987.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3988.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3989.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/399.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3990.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3991.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3992.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3993.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3994.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3995.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3996.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3997.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3998.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/3999.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/40.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/400.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4000.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4001.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4002.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4003.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4004.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4005.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4006.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4007.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4008.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4009.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/401.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4010.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4011.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4012.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4013.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4014.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4015.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4016.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4017.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4018.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4019.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/402.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4020.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4021.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4022.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4023.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4024.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4025.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4026.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4027.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4028.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4029.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/403.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4030.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4031.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4032.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4033.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4034.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4035.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4036.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4037.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4038.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4039.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/404.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4040.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4041.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4042.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4043.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4044.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4045.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4046.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4047.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4048.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4049.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/405.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4050.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4051.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4052.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4053.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4054.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4055.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4056.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4057.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4058.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4059.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/406.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4060.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4061.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4062.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4063.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4064.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4065.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4066.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4067.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4068.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4069.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/407.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4070.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4071.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4072.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4073.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4074.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4075.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4076.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4077.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4078.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4079.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/408.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4080.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4081.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4082.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4083.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4084.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4085.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4086.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4087.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4088.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4089.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/409.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4090.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4091.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4092.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4093.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4094.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4095.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4096.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4097.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4098.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4099.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/41.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/410.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4100.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4101.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4102.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4103.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4104.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4105.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4106.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4107.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4108.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4109.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/411.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4110.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4111.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4112.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4113.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4114.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4115.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4116.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4117.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4118.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4119.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/412.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4120.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4121.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4122.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4123.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4124.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4125.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4126.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4127.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4128.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4129.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/413.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4130.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4131.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4132.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4133.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4134.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4135.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4136.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4137.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4138.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4139.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/414.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4140.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4141.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4142.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4143.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4144.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4145.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4146.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4147.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4148.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4149.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/415.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4150.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4151.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4152.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4153.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4154.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4155.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4156.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4157.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4158.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4159.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/416.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4160.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4161.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4162.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4163.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4164.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4165.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4166.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4167.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4168.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4169.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/417.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4170.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4171.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4172.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4173.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4174.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4175.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4176.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4177.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4178.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4179.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/418.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4180.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4181.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4182.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4183.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4184.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4185.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4186.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4187.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4188.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4189.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/419.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4190.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4191.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4192.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4193.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4194.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4195.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4196.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4197.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4198.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4199.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/42.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/420.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4200.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4201.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4202.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4203.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4204.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4205.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4206.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4207.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4208.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4209.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/421.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4210.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4211.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4212.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4213.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4214.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4215.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4216.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4217.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4218.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4219.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/422.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4220.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4221.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4222.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4223.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4224.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4225.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4226.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4227.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4228.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4229.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/423.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4230.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4231.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4232.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4233.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4234.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4235.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4236.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4237.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4238.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4239.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/424.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4240.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4241.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4242.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4243.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4244.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4245.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4246.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4247.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4248.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4249.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/425.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4250.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4251.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4252.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4253.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4254.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4255.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4256.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4257.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4258.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4259.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/426.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4260.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4261.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4262.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4263.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4264.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4265.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4266.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4267.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4268.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4269.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/427.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4270.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4271.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4272.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4273.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4274.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4275.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4276.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4277.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4278.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4279.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/428.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4280.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4281.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4282.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4283.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4284.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4285.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4286.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4287.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4288.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4289.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/429.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4290.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4291.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4292.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4293.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4294.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4295.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4296.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4297.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4298.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4299.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/43.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/430.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4300.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4301.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4302.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4303.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4304.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4305.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4306.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4307.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4308.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4309.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/431.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4310.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4311.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4312.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4313.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4314.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4315.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4316.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4317.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4318.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4319.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/432.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4320.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4321.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4322.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4323.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4324.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4325.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4326.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4327.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4328.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4329.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/433.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4330.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4331.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4332.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4333.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4334.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4335.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4336.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4337.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4338.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4339.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/434.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4340.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4341.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4342.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4343.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4344.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4345.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4346.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4347.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4348.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4349.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/435.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4350.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4351.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4352.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4353.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4354.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4355.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4356.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4357.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4358.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4359.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/436.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4360.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4361.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4362.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4363.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4364.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4365.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4366.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4367.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4368.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4369.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/437.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4370.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4371.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4372.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4373.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4374.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4375.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4376.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4377.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4378.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4379.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/438.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4380.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4381.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4382.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4383.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4384.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4385.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4386.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4387.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4388.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4389.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/439.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4390.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4391.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4392.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4393.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4394.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4395.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4396.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4397.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4398.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4399.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/44.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/440.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4400.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4401.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4402.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4403.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4404.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4405.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4406.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4407.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4408.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4409.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/441.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4410.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4411.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4412.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4413.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4414.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4415.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4416.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4417.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4418.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4419.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/442.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4420.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4421.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4422.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4423.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4424.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4425.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4426.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4427.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4428.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4429.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/443.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4430.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4431.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4432.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4433.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4434.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4435.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4436.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4437.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4438.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4439.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/444.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4440.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4441.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4442.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4443.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4444.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4445.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4446.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4447.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4448.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4449.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/445.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4450.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4451.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4452.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4453.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4454.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4455.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4456.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4457.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4458.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4459.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/446.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4460.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4461.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4462.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4463.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4464.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4465.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4466.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4467.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4468.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4469.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/447.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4470.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4471.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4472.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4473.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4474.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4475.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4476.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4477.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4478.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4479.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/448.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4480.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4481.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4482.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4483.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4484.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4485.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4486.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4487.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4488.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4489.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/449.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4490.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4491.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4492.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4493.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4494.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4495.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4496.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4497.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4498.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4499.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/45.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/450.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4500.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4501.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4502.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4503.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4504.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4505.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4506.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4507.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4508.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4509.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/451.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4510.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4511.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4512.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4513.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4514.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4515.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4516.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4517.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4518.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4519.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/452.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4520.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4521.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4522.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4523.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4524.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4525.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4526.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4527.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4528.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4529.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/453.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4530.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4531.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4532.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4533.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4534.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4535.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4536.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4537.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4538.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4539.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/454.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4540.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4541.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4542.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4543.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4544.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4545.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4546.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4547.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4548.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4549.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/455.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4550.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4551.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4552.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4553.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4554.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4555.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4556.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4557.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4558.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4559.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/456.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4560.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4561.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4562.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4563.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4564.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4565.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4566.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4567.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4568.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4569.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/457.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4570.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4571.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4572.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4573.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4574.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4575.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4576.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4577.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4578.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4579.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/458.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4580.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4581.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4582.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4583.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4584.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4585.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4586.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4587.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4588.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4589.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/459.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4590.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4591.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4592.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4593.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4594.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4595.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4596.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4597.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4598.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4599.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/46.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/460.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4600.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4601.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4602.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4603.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4604.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4605.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4606.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4607.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4608.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4609.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/461.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4610.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4611.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4612.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4613.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4614.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4615.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4616.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4617.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4618.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4619.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/462.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4620.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4621.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4622.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4623.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4624.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4625.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4626.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4627.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4628.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4629.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/463.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4630.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4631.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4632.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4633.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4634.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4635.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4636.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4637.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4638.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4639.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/464.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4640.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4641.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4642.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4643.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4644.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4645.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4646.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4647.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4648.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4649.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/465.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4650.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4651.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4652.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4653.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4654.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4655.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4656.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4657.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4658.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4659.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/466.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4660.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4661.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4662.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4663.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4664.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4665.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4666.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4667.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4668.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4669.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/467.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4670.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4671.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4672.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4673.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4674.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4675.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4676.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4677.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4678.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4679.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/468.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4680.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4681.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4682.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4683.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4684.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4685.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4686.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4687.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4688.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4689.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/469.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4690.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4691.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4692.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4693.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4694.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4695.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4696.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4697.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4698.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4699.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/47.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/470.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4700.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4701.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4702.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4703.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4704.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4705.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4706.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4707.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4708.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4709.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/471.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4710.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4711.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4712.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4713.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4714.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4715.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4716.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4717.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4718.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4719.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/472.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4720.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4721.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4722.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4723.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4724.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4725.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4726.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4727.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4728.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4729.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/473.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4730.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4731.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4732.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4733.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4734.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4735.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4736.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4737.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4738.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4739.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/474.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4740.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4741.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4742.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4743.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4744.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4745.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4746.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4747.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4748.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4749.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/475.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4750.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4751.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4752.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4753.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4754.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4755.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4756.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4757.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4758.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4759.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/476.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4760.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4761.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4762.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4763.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4764.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4765.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4766.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4767.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4768.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4769.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/477.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4770.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4771.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4772.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4773.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4774.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4775.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4776.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4777.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4778.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4779.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/478.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4780.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4781.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4782.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4783.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4784.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4785.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4786.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4787.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4788.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4789.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/479.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4790.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4791.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4792.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4793.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4794.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4795.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4796.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4797.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4798.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4799.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/48.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/480.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4800.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4801.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4802.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4803.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4804.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4805.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4806.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4807.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4808.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4809.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/481.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4810.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4811.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4812.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4813.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4814.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4815.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4816.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4817.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4818.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4819.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/482.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4820.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4821.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4822.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4823.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4824.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4825.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4826.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4827.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4828.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4829.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/483.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4830.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4831.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4832.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4833.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4834.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4835.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4836.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4837.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4838.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4839.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/484.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4840.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4841.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4842.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4843.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4844.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4845.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4846.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4847.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4848.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4849.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/485.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4850.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4851.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4852.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4853.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4854.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4855.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4856.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4857.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4858.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4859.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/486.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4860.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4861.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4862.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4863.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4864.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4865.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4866.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4867.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4868.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4869.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/487.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4870.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4871.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4872.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4873.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4874.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4875.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4876.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4877.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4878.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4879.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/488.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4880.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4881.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4882.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4883.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4884.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4885.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4886.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4887.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4888.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4889.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/489.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4890.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4891.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4892.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4893.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4894.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4895.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4896.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4897.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4898.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4899.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/49.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/490.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4900.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4901.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4902.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4903.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4904.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4905.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4906.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4907.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4908.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4909.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/491.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4910.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4911.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4912.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4913.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4914.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4915.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4916.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4917.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4918.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4919.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/492.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4920.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4921.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4922.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4923.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4924.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4925.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4926.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4927.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4928.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4929.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/493.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4930.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4931.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4932.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4933.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4934.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4935.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4936.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4937.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4938.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4939.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/494.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4940.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4941.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4942.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4943.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4944.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4945.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4946.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4947.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4948.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4949.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/495.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4950.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4951.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4952.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4953.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4954.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4955.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4956.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4957.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4958.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4959.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/496.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4960.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4961.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4962.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4963.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4964.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4965.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4966.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4967.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4968.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4969.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/497.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4970.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4971.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4972.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4973.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4974.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4975.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4976.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4977.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4978.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4979.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/498.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4980.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4981.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4982.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4983.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4984.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4985.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4986.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4987.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4988.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4989.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/499.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4990.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4991.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4992.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4993.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4994.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4995.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4996.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4997.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4998.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/4999.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/50.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/500.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5000.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5001.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5002.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5003.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5004.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5005.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5006.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5007.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5008.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5009.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/501.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5010.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5011.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5012.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5013.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5014.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5015.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5016.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5017.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5018.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5019.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/502.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5020.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5021.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5022.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5023.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5024.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5025.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5026.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5027.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5028.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5029.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/503.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5030.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5031.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5032.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5033.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5034.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5035.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5036.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5037.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5038.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5039.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/504.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5040.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5041.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5042.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5043.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5044.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5045.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5046.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5047.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5048.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5049.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/505.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5050.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5051.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5052.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5053.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5054.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5055.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5056.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5057.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5058.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5059.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/506.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5060.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5061.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5062.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5063.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5064.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5065.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5066.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5067.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5068.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5069.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/507.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5070.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5071.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5072.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5073.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5074.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5075.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5076.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5077.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5078.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5079.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/508.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5080.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5081.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5082.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5083.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5084.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5085.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5086.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5087.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5088.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5089.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/509.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5090.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5091.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5092.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5093.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5094.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5095.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5096.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5097.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5098.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5099.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/51.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/510.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5100.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5101.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5102.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5103.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5104.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5105.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5106.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5107.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5108.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5109.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/511.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5110.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5111.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5112.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5113.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5114.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5115.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5116.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5117.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5118.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5119.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/512.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5120.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5121.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5122.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5123.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5124.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5125.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5126.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5127.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5128.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5129.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/513.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5130.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5131.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5132.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5133.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5134.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5135.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5136.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5137.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5138.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5139.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/514.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5140.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5141.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5142.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5143.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5144.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5145.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5146.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5147.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5148.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5149.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/515.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5150.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5151.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5152.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5153.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5154.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5155.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5156.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5157.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5158.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5159.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/516.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5160.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5161.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5162.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5163.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5164.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5165.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5166.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5167.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5168.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5169.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/517.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5170.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5171.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5172.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5173.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5174.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5175.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5176.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5177.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5178.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5179.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/518.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5180.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5181.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5182.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5183.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5184.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5185.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5186.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5187.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5188.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5189.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/519.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5190.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5191.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5192.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5193.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5194.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5195.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5196.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5197.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5198.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5199.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/52.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/520.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5200.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5201.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5202.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5203.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5204.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5205.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5206.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5207.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5208.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5209.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/521.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5210.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5211.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5212.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5213.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5214.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5215.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5216.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5217.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5218.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5219.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/522.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5220.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5221.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5222.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5223.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5224.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5225.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5226.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5227.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5228.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5229.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/523.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5230.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5231.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5232.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5233.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5234.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5235.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5236.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5237.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5238.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5239.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/524.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5240.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5241.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5242.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5243.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5244.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5245.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5246.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5247.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5248.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5249.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/525.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5250.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5251.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5252.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5253.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5254.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5255.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5256.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5257.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5258.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5259.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/526.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5260.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5261.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5262.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5263.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5264.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5265.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5266.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5267.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5268.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5269.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/527.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5270.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5271.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5272.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5273.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5274.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5275.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5276.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5277.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5278.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5279.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/528.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5280.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5281.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5282.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5283.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5284.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5285.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5286.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5287.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5288.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5289.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/529.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5290.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5291.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5292.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5293.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5294.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5295.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5296.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5297.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5298.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5299.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/53.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/530.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5300.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5301.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5302.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5303.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5304.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5305.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5306.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5307.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5308.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5309.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/531.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5310.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5311.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5312.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5313.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5314.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5315.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5316.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5317.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5318.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5319.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/532.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5320.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5321.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5322.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5323.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5324.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5325.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5326.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5327.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5328.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5329.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/533.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5330.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5331.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5332.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5333.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5334.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5335.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5336.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5337.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5338.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5339.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/534.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5340.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5341.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5342.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5343.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5344.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5345.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5346.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5347.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5348.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5349.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/535.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5350.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5351.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5352.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5353.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5354.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5355.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5356.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5357.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5358.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5359.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/536.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5360.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5361.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5362.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5363.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5364.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5365.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5366.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5367.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5368.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5369.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/537.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5370.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5371.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5372.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5373.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5374.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5375.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5376.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5377.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5378.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5379.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/538.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5380.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5381.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5382.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5383.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5384.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5385.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5386.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5387.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5388.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5389.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/539.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5390.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5391.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5392.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5393.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5394.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5395.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5396.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5397.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5398.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5399.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/54.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/540.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5400.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5401.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5402.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5403.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5404.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5405.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5406.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5407.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5408.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5409.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/541.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5410.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5411.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5412.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5413.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5414.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5415.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5416.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5417.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5418.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5419.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/542.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5420.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5421.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5422.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5423.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5424.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5425.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5426.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5427.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5428.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5429.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/543.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5430.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5431.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5432.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5433.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5434.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5435.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5436.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5437.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5438.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5439.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/544.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5440.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5441.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5442.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5443.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5444.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5445.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5446.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5447.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5448.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5449.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/545.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5450.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5451.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5452.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5453.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5454.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/5455.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/546.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/547.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/548.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/549.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/55.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/550.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/551.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/552.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/553.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/554.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/555.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/556.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/557.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/558.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/559.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/56.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/560.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/561.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/562.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/563.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/564.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/565.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/566.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/567.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/568.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/569.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/57.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/570.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/571.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/572.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/573.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/574.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/575.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/576.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/577.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/578.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/579.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/58.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/580.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/581.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/582.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/583.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/584.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/585.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/586.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/587.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/588.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/589.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/59.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/590.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/591.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/592.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/593.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/594.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/595.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/596.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/597.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/598.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/599.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/6.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/60.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/600.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/601.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/602.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/603.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/604.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/605.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/606.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/607.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/608.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/609.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/61.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/610.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/611.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/612.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/613.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/614.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/615.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/616.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/617.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/618.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/619.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/62.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/620.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/621.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/622.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/623.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/624.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/625.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/626.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/627.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/628.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/629.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/63.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/630.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/631.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/632.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/633.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/634.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/635.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/636.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/637.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/638.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/639.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/64.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/640.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/641.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/642.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/643.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/644.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/645.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/646.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/647.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/648.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/649.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/65.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/650.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/651.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/652.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/653.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/654.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/655.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/656.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/657.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/658.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/659.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/66.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/660.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/661.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/662.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/663.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/664.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/665.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/666.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/667.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/668.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/669.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/67.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/670.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/671.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/672.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/673.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/674.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/675.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/676.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/677.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/678.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/679.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/68.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/680.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/681.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/682.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/683.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/684.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/685.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/686.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/687.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/688.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/689.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/69.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/690.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/691.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/692.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/693.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/694.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/695.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/696.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/697.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/698.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/699.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/7.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/70.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/700.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/701.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/702.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/703.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/704.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/705.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/706.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/707.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/708.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/709.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/71.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/710.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/711.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/712.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/713.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/714.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/715.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/716.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/717.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/718.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/719.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/72.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/720.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/721.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/722.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/723.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/724.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/725.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/726.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/727.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/728.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/729.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/73.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/730.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/731.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/732.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/733.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/734.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/735.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/736.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/737.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/738.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/739.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/74.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/740.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/741.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/742.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/743.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/744.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/745.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/746.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/747.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/748.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/749.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/75.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/750.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/751.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/752.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/753.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/754.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/755.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/756.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/757.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/758.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/759.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/76.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/760.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/761.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/762.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/763.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/764.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/765.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/766.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/767.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/768.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/769.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/77.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/770.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/771.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/772.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/773.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/774.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/775.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/776.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/777.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/778.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/779.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/78.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/780.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/781.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/782.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/783.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/784.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/785.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/786.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/787.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/788.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/789.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/79.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/790.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/791.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/792.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/793.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/794.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/795.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/796.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/797.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/798.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/799.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/8.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/80.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/800.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/801.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/802.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/803.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/804.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/805.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/806.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/807.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/808.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/809.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/81.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/810.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/811.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/812.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/813.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/814.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/815.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/816.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/817.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/818.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/819.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/82.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/820.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/821.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/822.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/823.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/824.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/825.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/826.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/827.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/828.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/829.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/83.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/830.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/831.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/832.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/833.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/834.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/835.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/836.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/837.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/838.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/839.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/84.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/840.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/841.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/842.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/843.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/844.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/845.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/846.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/847.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/848.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/849.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/85.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/850.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/851.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/852.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/853.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/854.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/855.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/856.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/857.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/858.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/859.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/86.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/860.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/861.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/862.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/863.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/864.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/865.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/866.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/867.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/868.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/869.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/87.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/870.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/871.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/872.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/873.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/874.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/875.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/876.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/877.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/878.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/879.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/88.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/880.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/881.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/882.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/883.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/884.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/885.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/886.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/887.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/888.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/889.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/89.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/890.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/891.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/892.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/893.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/894.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/895.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/896.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/897.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/898.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/899.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/9.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/90.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/900.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/901.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/902.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/903.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/904.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/905.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/906.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/907.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/908.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/909.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/91.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/910.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/911.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/912.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/913.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/914.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/915.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/916.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/917.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/918.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/919.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/92.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/920.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/921.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/922.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/923.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/924.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/925.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/926.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/927.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/928.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/929.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/93.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/930.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/931.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/932.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/933.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/934.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/935.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/936.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/937.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/938.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/939.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/94.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/940.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/941.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/942.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/943.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/944.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/945.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/946.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/947.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/948.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/949.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/95.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/950.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/951.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/952.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/953.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/954.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/955.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/956.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/957.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/958.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/959.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/96.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/960.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/961.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/962.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/963.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/964.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/965.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/966.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/967.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/968.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/969.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/97.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/970.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/971.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/972.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/973.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/974.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/975.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/976.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/977.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/978.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/979.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/98.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/980.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/981.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/982.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/983.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/984.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/985.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/986.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/987.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/988.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/989.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/99.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/990.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/991.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/992.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/993.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/994.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/995.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/996.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/997.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/998.png")]
[assembly: AssemblyAssociatedContentFile("assets/vanilla/icons/999.png")]
[assembly: TargetFramework(".NETCoreApp,Version=v10.0", FrameworkDisplayName = ".NET 10.0")]
[assembly: AssemblyCompany("TerrasavrNative.App")]
[assembly: AssemblyConfiguration("Debug")]
[assembly: AssemblyDescription("Editor de personajes de Terraria (vanilla + Calamity Mod)")]
[assembly: AssemblyFileVersion("1.2.0.0")]
[assembly: AssemblyInformationalVersion("1.2.0+eccc2fa75b92681ed37a1d56332076e1dc2a8c2b")]
[assembly: AssemblyProduct("Terrakeep")]
[assembly: AssemblyTitle("Terrakeep")]
[assembly: TargetPlatform("Windows7.0")]
[assembly: SupportedOSPlatform("Windows7.0")]
[assembly: AssemblyVersion("1.2.0.0")]
[module: RefSafetyRules(11)]
[CompilerGenerated]
internal sealed class <>z__ReadOnlyArray<T> : IEnumerable, ICollection, IList, IEnumerable<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection<T>, IList<T>
{
	[CompilerGenerated]
	private readonly T[] _items;

	int ICollection.Count => _items.Length;

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot => this;

	object? IList.this[int index]
	{
		get
		{
			return _items[index];
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	bool IList.IsFixedSize => true;

	bool IList.IsReadOnly => true;

	int IReadOnlyCollection<T>.Count => _items.Length;

	T IReadOnlyList<T>.this[int index] => _items[index];

	int ICollection<T>.Count => _items.Length;

	bool ICollection<T>.IsReadOnly => true;

	T IList<T>.this[int index]
	{
		get
		{
			return _items[index];
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	public <>z__ReadOnlyArray(T[] items)
	{
		_items = items;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable)_items).GetEnumerator();
	}

	void ICollection.CopyTo(Array array, int index)
	{
		((ICollection)_items).CopyTo(array, index);
	}

	int IList.Add(object? value)
	{
		throw new NotSupportedException();
	}

	void IList.Clear()
	{
		throw new NotSupportedException();
	}

	bool IList.Contains(object? value)
	{
		return ((IList)_items).Contains(value);
	}

	int IList.IndexOf(object? value)
	{
		return ((IList)_items).IndexOf(value);
	}

	void IList.Insert(int index, object? value)
	{
		throw new NotSupportedException();
	}

	void IList.Remove(object? value)
	{
		throw new NotSupportedException();
	}

	void IList.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return ((IEnumerable<T>)_items).GetEnumerator();
	}

	void ICollection<T>.Add(T item)
	{
		throw new NotSupportedException();
	}

	void ICollection<T>.Clear()
	{
		throw new NotSupportedException();
	}

	bool ICollection<T>.Contains(T item)
	{
		return ((ICollection<T>)_items).Contains(item);
	}

	void ICollection<T>.CopyTo(T[] array, int arrayIndex)
	{
		((ICollection<T>)_items).CopyTo(array, arrayIndex);
	}

	bool ICollection<T>.Remove(T item)
	{
		throw new NotSupportedException();
	}

	int IList<T>.IndexOf(T item)
	{
		return ((IList<T>)_items).IndexOf(item);
	}

	void IList<T>.Insert(int index, T item)
	{
		throw new NotSupportedException();
	}

	void IList<T>.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}
}
[CompilerGenerated]
internal sealed class <>z__ReadOnlySingleElementList<T> : IEnumerable, ICollection, IList, IEnumerable<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection<T>, IList<T>
{
	private sealed class Enumerator : IDisposable, IEnumerator, IEnumerator<T>
	{
		[CompilerGenerated]
		private readonly T _item;

		[CompilerGenerated]
		private bool _moveNextCalled;

		object IEnumerator.Current => _item;

		T IEnumerator<T>.Current => _item;

		public Enumerator(T item)
		{
			_item = item;
		}

		bool IEnumerator.MoveNext()
		{
			return !_moveNextCalled && (_moveNextCalled = true);
		}

		void IEnumerator.Reset()
		{
			_moveNextCalled = false;
		}

		void IDisposable.Dispose()
		{
		}
	}

	[CompilerGenerated]
	private readonly T _item;

	int ICollection.Count => 1;

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot => this;

	object? IList.this[int index]
	{
		get
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			return _item;
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	bool IList.IsFixedSize => true;

	bool IList.IsReadOnly => true;

	int IReadOnlyCollection<T>.Count => 1;

	T IReadOnlyList<T>.this[int index]
	{
		get
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			return _item;
		}
	}

	int ICollection<T>.Count => 1;

	bool ICollection<T>.IsReadOnly => true;

	T IList<T>.this[int index]
	{
		get
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			return _item;
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	public <>z__ReadOnlySingleElementList(T item)
	{
		_item = item;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new Enumerator(_item);
	}

	void ICollection.CopyTo(Array array, int index)
	{
		array.SetValue(_item, index);
	}

	int IList.Add(object? value)
	{
		throw new NotSupportedException();
	}

	void IList.Clear()
	{
		throw new NotSupportedException();
	}

	bool IList.Contains(object? value)
	{
		return EqualityComparer<T>.Default.Equals(_item, (T)value);
	}

	int IList.IndexOf(object? value)
	{
		return (!EqualityComparer<T>.Default.Equals(_item, (T)value)) ? (-1) : 0;
	}

	void IList.Insert(int index, object? value)
	{
		throw new NotSupportedException();
	}

	void IList.Remove(object? value)
	{
		throw new NotSupportedException();
	}

	void IList.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return new Enumerator(_item);
	}

	void ICollection<T>.Add(T item)
	{
		throw new NotSupportedException();
	}

	void ICollection<T>.Clear()
	{
		throw new NotSupportedException();
	}

	bool ICollection<T>.Contains(T item)
	{
		return EqualityComparer<T>.Default.Equals(_item, item);
	}

	void ICollection<T>.CopyTo(T[] array, int arrayIndex)
	{
		array[arrayIndex] = _item;
	}

	bool ICollection<T>.Remove(T item)
	{
		throw new NotSupportedException();
	}

	int IList<T>.IndexOf(T item)
	{
		return (!EqualityComparer<T>.Default.Equals(_item, item)) ? (-1) : 0;
	}

	void IList<T>.Insert(int index, T item)
	{
		throw new NotSupportedException();
	}

	void IList<T>.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}
}
namespace CommunityToolkit.Mvvm.ComponentModel.__Internals
{
	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[DebuggerNonUserCode]
	[ExcludeFromCodeCoverage]
	[EditorBrowsable(EditorBrowsableState.Never)]
	[Obsolete("This type is not intended to be used directly by user code")]
	internal static class __KnownINotifyPropertyChangingArgs
	{
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs PreviewImage = new PropertyChangingEventArgs("PreviewImage");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs HairStyle = new PropertyChangingEventArgs("HairStyle");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs HairDye = new PropertyChangingEventArgs("HairDye");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs HairDyeDisplayName = new PropertyChangingEventArgs("HairDyeDisplayName");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs IsMale = new PropertyChangingEventArgs("IsMale");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs IsHairPickerOpen = new PropertyChangingEventArgs("IsHairPickerOpen");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs IsHairDyePickerOpen = new PropertyChangingEventArgs("IsHairDyePickerOpen");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs Difficulty = new PropertyChangingEventArgs("Difficulty");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs HealthNow = new PropertyChangingEventArgs("HealthNow");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs HealthMax = new PropertyChangingEventArgs("HealthMax");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs ManaNow = new PropertyChangingEventArgs("ManaNow");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs ManaMax = new PropertyChangingEventArgs("ManaMax");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs FishingQuestsCompleted = new PropertyChangingEventArgs("FishingQuestsCompleted");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs GolferScore = new PropertyChangingEventArgs("GolferScore");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs PlayHours = new PropertyChangingEventArgs("PlayHours");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs DurationSeconds = new PropertyChangingEventArgs("DurationSeconds");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs SearchText = new PropertyChangingEventArgs("SearchText");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs ResultsSummary = new PropertyChangingEventArgs("ResultsSummary");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs IsPicking = new PropertyChangingEventArgs("IsPicking");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs ItemCount = new PropertyChangingEventArgs("ItemCount");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs IconPath = new PropertyChangingEventArgs("IconPath");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs IsSelected = new PropertyChangingEventArgs("IsSelected");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs IsExpanded = new PropertyChangingEventArgs("IsExpanded");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs R = new PropertyChangingEventArgs("R");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs G = new PropertyChangingEventArgs("G");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs B = new PropertyChangingEventArgs("B");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs Preview = new PropertyChangingEventArgs("Preview");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs SelectedLoadout = new PropertyChangingEventArgs("SelectedLoadout");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs SelectedKind = new PropertyChangingEventArgs("SelectedKind");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs WorldImage = new PropertyChangingEventArgs("WorldImage");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs StatusMessage = new PropertyChangingEventArgs("StatusMessage");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs WorldTitle = new PropertyChangingEventArgs("WorldTitle");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs IsWorldLoaded = new PropertyChangingEventArgs("IsWorldLoaded");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs NpcSearchText = new PropertyChangingEventArgs("NpcSearchText");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs Zoom = new PropertyChangingEventArgs("Zoom");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs HoverInfo = new PropertyChangingEventArgs("HoverInfo");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs ExtraAccessory = new PropertyChangingEventArgs("ExtraAccessory");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs UnlockedBiomeTorches = new PropertyChangingEventArgs("UnlockedBiomeTorches");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs UsingBiomeTorches = new PropertyChangingEventArgs("UsingBiomeTorches");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs ArtisanBread = new PropertyChangingEventArgs("ArtisanBread");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs VitalCrystal = new PropertyChangingEventArgs("VitalCrystal");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs AegisFruit = new PropertyChangingEventArgs("AegisFruit");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs ArcaneCrystal = new PropertyChangingEventArgs("ArcaneCrystal");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs GalaxyPearl = new PropertyChangingEventArgs("GalaxyPearl");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs GummyWorm = new PropertyChangingEventArgs("GummyWorm");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs Ambrosia = new PropertyChangingEventArgs("Ambrosia");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs FinishedDD2Event = new PropertyChangingEventArgs("FinishedDD2Event");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs UnlockedSuperMinecart = new PropertyChangingEventArgs("UnlockedSuperMinecart");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs UsingSuperMinecart = new PropertyChangingEventArgs("UsingSuperMinecart");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs Slot = new PropertyChangingEventArgs("Slot");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs SelectedMeta = new PropertyChangingEventArgs("SelectedMeta");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs SelectedGroup = new PropertyChangingEventArgs("SelectedGroup");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs CanHavePrefix = new PropertyChangingEventArgs("CanHavePrefix");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs NoPrefixMessage = new PropertyChangingEventArgs("NoPrefixMessage");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs CategoriesLabel = new PropertyChangingEventArgs("CategoriesLabel");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs HasSelection = new PropertyChangingEventArgs("HasSelection");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs DisplayName = new PropertyChangingEventArgs("DisplayName");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs Count = new PropertyChangingEventArgs("Count");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs IsCalamity = new PropertyChangingEventArgs("IsCalamity");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs IsEmpty = new PropertyChangingEventArgs("IsEmpty");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs PrefixDisplay = new PropertyChangingEventArgs("PrefixDisplay");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs HasBestPrefixSuggestion = new PropertyChangingEventArgs("HasBestPrefixSuggestion");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs StatsTooltip = new PropertyChangingEventArgs("StatsTooltip");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs ItemId = new PropertyChangingEventArgs("ItemId");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs PrefixId = new PropertyChangingEventArgs("PrefixId");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs PickTarget = new PropertyChangingEventArgs("PickTarget");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs SelectedCategory = new PropertyChangingEventArgs("SelectedCategory");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs CharacterName = new PropertyChangingEventArgs("CharacterName");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs IsCharacterLoaded = new PropertyChangingEventArgs("IsCharacterLoaded");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs HasCalamityData = new PropertyChangingEventArgs("HasCalamityData");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs SelectedTabIndex = new PropertyChangingEventArgs("SelectedTabIndex");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs PersonajeInnerTabIndex = new PropertyChangingEventArgs("PersonajeInnerTabIndex");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs SaveConfirmationVisible = new PropertyChangingEventArgs("SaveConfirmationVisible");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs EquipmentGroup = new PropertyChangingEventArgs("EquipmentGroup");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs Name = new PropertyChangingEventArgs("Name");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs SpawnX = new PropertyChangingEventArgs("SpawnX");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs SpawnY = new PropertyChangingEventArgs("SpawnY");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs Address = new PropertyChangingEventArgs("Address");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangingEventArgs RawVersion = new PropertyChangingEventArgs("RawVersion");
	}
	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[DebuggerNonUserCode]
	[ExcludeFromCodeCoverage]
	[EditorBrowsable(EditorBrowsableState.Never)]
	[Obsolete("This type is not intended to be used directly by user code")]
	internal static class __KnownINotifyPropertyChangedArgs
	{
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs PreviewImage = new PropertyChangedEventArgs("PreviewImage");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs HairStyle = new PropertyChangedEventArgs("HairStyle");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs HairDye = new PropertyChangedEventArgs("HairDye");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs HairDyeDisplayName = new PropertyChangedEventArgs("HairDyeDisplayName");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs IsMale = new PropertyChangedEventArgs("IsMale");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs IsHairPickerOpen = new PropertyChangedEventArgs("IsHairPickerOpen");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs IsHairDyePickerOpen = new PropertyChangedEventArgs("IsHairDyePickerOpen");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs Difficulty = new PropertyChangedEventArgs("Difficulty");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs HealthNow = new PropertyChangedEventArgs("HealthNow");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs HealthMax = new PropertyChangedEventArgs("HealthMax");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs ManaNow = new PropertyChangedEventArgs("ManaNow");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs ManaMax = new PropertyChangedEventArgs("ManaMax");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs FishingQuestsCompleted = new PropertyChangedEventArgs("FishingQuestsCompleted");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs GolferScore = new PropertyChangedEventArgs("GolferScore");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs PlayHours = new PropertyChangedEventArgs("PlayHours");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs DurationSeconds = new PropertyChangedEventArgs("DurationSeconds");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs SearchText = new PropertyChangedEventArgs("SearchText");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs ResultsSummary = new PropertyChangedEventArgs("ResultsSummary");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs IsPicking = new PropertyChangedEventArgs("IsPicking");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs ItemCount = new PropertyChangedEventArgs("ItemCount");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs IconPath = new PropertyChangedEventArgs("IconPath");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs IsSelected = new PropertyChangedEventArgs("IsSelected");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs IsExpanded = new PropertyChangedEventArgs("IsExpanded");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs R = new PropertyChangedEventArgs("R");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs G = new PropertyChangedEventArgs("G");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs B = new PropertyChangedEventArgs("B");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs Preview = new PropertyChangedEventArgs("Preview");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs SelectedLoadout = new PropertyChangedEventArgs("SelectedLoadout");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs SelectedKind = new PropertyChangedEventArgs("SelectedKind");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs WorldImage = new PropertyChangedEventArgs("WorldImage");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs StatusMessage = new PropertyChangedEventArgs("StatusMessage");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs WorldTitle = new PropertyChangedEventArgs("WorldTitle");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs IsWorldLoaded = new PropertyChangedEventArgs("IsWorldLoaded");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs NpcSearchText = new PropertyChangedEventArgs("NpcSearchText");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs Zoom = new PropertyChangedEventArgs("Zoom");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs HoverInfo = new PropertyChangedEventArgs("HoverInfo");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs ExtraAccessory = new PropertyChangedEventArgs("ExtraAccessory");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs UnlockedBiomeTorches = new PropertyChangedEventArgs("UnlockedBiomeTorches");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs UsingBiomeTorches = new PropertyChangedEventArgs("UsingBiomeTorches");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs ArtisanBread = new PropertyChangedEventArgs("ArtisanBread");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs VitalCrystal = new PropertyChangedEventArgs("VitalCrystal");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs AegisFruit = new PropertyChangedEventArgs("AegisFruit");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs ArcaneCrystal = new PropertyChangedEventArgs("ArcaneCrystal");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs GalaxyPearl = new PropertyChangedEventArgs("GalaxyPearl");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs GummyWorm = new PropertyChangedEventArgs("GummyWorm");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs Ambrosia = new PropertyChangedEventArgs("Ambrosia");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs FinishedDD2Event = new PropertyChangedEventArgs("FinishedDD2Event");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs UnlockedSuperMinecart = new PropertyChangedEventArgs("UnlockedSuperMinecart");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs UsingSuperMinecart = new PropertyChangedEventArgs("UsingSuperMinecart");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs Slot = new PropertyChangedEventArgs("Slot");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs SelectedMeta = new PropertyChangedEventArgs("SelectedMeta");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs SelectedGroup = new PropertyChangedEventArgs("SelectedGroup");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs CanHavePrefix = new PropertyChangedEventArgs("CanHavePrefix");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs NoPrefixMessage = new PropertyChangedEventArgs("NoPrefixMessage");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs CategoriesLabel = new PropertyChangedEventArgs("CategoriesLabel");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs HasSelection = new PropertyChangedEventArgs("HasSelection");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs DisplayName = new PropertyChangedEventArgs("DisplayName");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs Count = new PropertyChangedEventArgs("Count");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs IsCalamity = new PropertyChangedEventArgs("IsCalamity");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs IsEmpty = new PropertyChangedEventArgs("IsEmpty");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs PrefixDisplay = new PropertyChangedEventArgs("PrefixDisplay");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs HasBestPrefixSuggestion = new PropertyChangedEventArgs("HasBestPrefixSuggestion");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs StatsTooltip = new PropertyChangedEventArgs("StatsTooltip");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs ItemId = new PropertyChangedEventArgs("ItemId");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs PrefixId = new PropertyChangedEventArgs("PrefixId");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs PickTarget = new PropertyChangedEventArgs("PickTarget");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs SelectedCategory = new PropertyChangedEventArgs("SelectedCategory");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs CharacterName = new PropertyChangedEventArgs("CharacterName");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs IsCharacterLoaded = new PropertyChangedEventArgs("IsCharacterLoaded");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs HasCalamityData = new PropertyChangedEventArgs("HasCalamityData");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs SelectedTabIndex = new PropertyChangedEventArgs("SelectedTabIndex");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs PersonajeInnerTabIndex = new PropertyChangedEventArgs("PersonajeInnerTabIndex");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs SaveConfirmationVisible = new PropertyChangedEventArgs("SaveConfirmationVisible");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs EquipmentGroup = new PropertyChangedEventArgs("EquipmentGroup");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs Name = new PropertyChangedEventArgs("Name");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs SpawnX = new PropertyChangedEventArgs("SpawnX");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs SpawnY = new PropertyChangedEventArgs("SpawnY");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs Address = new PropertyChangedEventArgs("Address");

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This field is not intended to be referenced directly by user code")]
		public static readonly PropertyChangedEventArgs RawVersion = new PropertyChangedEventArgs("RawVersion");
	}
}
namespace TerrasavrNative.App
{
	public class App : Application
	{
		private bool _contentLoaded;

		protected override void OnStartup(StartupEventArgs e)
		{
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Expected O, but got Unknown
			base.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(OnDispatcherUnhandledException);
			AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
			base.OnStartup(e);
		}

		private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			LogAndShow(e.Exception);
			e.Handled = true;
		}

		private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			if (e.ExceptionObject is Exception ex)
			{
				LogAndShow(ex);
			}
		}

		private static void LogAndShow(Exception ex)
		{
			string text = Path.Combine(AppContext.BaseDirectory, "ultimo-error.log");
			try
			{
				File.WriteAllText(text, $"{DateTime.Now}\n{ex}");
			}
			catch
			{
			}
			MessageBox.Show($"Terrakeep encontro un error inesperado y esta pantalla puede no funcionar bien.\n\n{ex.GetType().Name}: {ex.Message}\n\nDetalle guardado en:\n{text}", "Error inesperado - Terrakeep", MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}

		[DebuggerNonUserCode]
		[GeneratedCode("PresentationBuildTasks", "10.0.11.0")]
		public void InitializeComponent()
		{
			if (!_contentLoaded)
			{
				_contentLoaded = true;
				base.StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);
				Uri resourceLocator = new Uri("/TerrasavrNative.App;V1.2.0.0;component/app.xaml", UriKind.Relative);
				Application.LoadComponent(this, resourceLocator);
			}
		}

		[STAThread]
		[DebuggerNonUserCode]
		[GeneratedCode("PresentationBuildTasks", "10.0.11.0")]
		public static void Main()
		{
			App app = new App();
			app.InitializeComponent();
			app.Run();
		}
	}
	public class MainWindow : Window, IComponentConnector, IStyleConnector
	{
		private readonly MainViewModel _viewModel = new MainViewModel();

		private Point? _mapDragStart;

		private double _mapDragStartH;

		private double _mapDragStartV;

		private Point? _dragStartLibrary;

		private Point? _dragStartSlot;

		internal ScrollViewer WorldMapScroll;

		internal Image WorldMapImage;

		internal Canvas MapTooltipCanvas;

		internal Border MapTooltipBorder;

		private bool _contentLoaded;

		public MainWindow()
		{
			InitializeComponent();
			base.DataContext = _viewModel;
			_viewModel.Exploration.NavigateToTileRequested += OnNavigateToTile;
		}

		private void OnLoadClick(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Title = "Cargar personaje de Terraria",
				Filter = "Personaje de Terraria (*.plr)|*.plr|Todos los archivos (*.*)|*.*",
				InitialDirectory = GetDefaultPlayersDirectory()
			};
			if (openFileDialog.ShowDialog(this) == true)
			{
				_viewModel.LoadFromPath(openFileDialog.FileName);
			}
		}

		private static string GetDefaultPlayersDirectory()
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			InlineArray5<string> buffer = default(InlineArray5<string>);
			buffer[0] = folderPath;
			buffer[1] = "My Games";
			buffer[2] = "Terraria";
			buffer[3] = "tModLoader";
			buffer[4] = "Players";
			string text = Path.Combine(buffer);
			return Directory.Exists(text) ? text : folderPath;
		}

		private void OnLoadWorldClick(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Title = "Cargar mundo de Terraria",
				Filter = "Mundo de Terraria (*.wld)|*.wld|Todos los archivos (*.*)|*.*",
				InitialDirectory = GetDefaultWorldsDirectory()
			};
			if (openFileDialog.ShowDialog(this) == true)
			{
				_viewModel.Exploration.LoadFromPath(openFileDialog.FileName);
			}
		}

		private static string GetDefaultWorldsDirectory()
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			InlineArray5<string> buffer = default(InlineArray5<string>);
			buffer[0] = folderPath;
			buffer[1] = "My Games";
			buffer[2] = "Terraria";
			buffer[3] = "tModLoader";
			buffer[4] = "Worlds";
			string text = Path.Combine(buffer);
			return Directory.Exists(text) ? text : folderPath;
		}

		private void OnWorldMapPreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			double zoom = _viewModel.Exploration.Zoom;
			Point position = e.GetPosition(WorldMapScroll);
			double num = (WorldMapScroll.HorizontalOffset + ((Point)(ref position)).X) / zoom;
			double num2 = (WorldMapScroll.VerticalOffset + ((Point)(ref position)).Y) / zoom;
			_viewModel.Exploration.Zoom = zoom * ((e.Delta > 0) ? 1.15 : 0.8695652173913044);
			double zoom2 = _viewModel.Exploration.Zoom;
			WorldMapScroll.UpdateLayout();
			WorldMapScroll.ScrollToHorizontalOffset(num * zoom2 - ((Point)(ref position)).X);
			WorldMapScroll.ScrollToVerticalOffset(num2 * zoom2 - ((Point)(ref position)).Y);
			e.Handled = true;
		}

		private void OnWorldMapMouseDown(object sender, MouseButtonEventArgs e)
		{
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			_mapDragStart = e.GetPosition(WorldMapScroll);
			_mapDragStartH = WorldMapScroll.HorizontalOffset;
			_mapDragStartV = WorldMapScroll.VerticalOffset;
			WorldMapScroll.CaptureMouse();
		}

		private void OnWorldMapMouseUp(object sender, MouseButtonEventArgs e)
		{
			_mapDragStart = null;
			WorldMapScroll.ReleaseMouseCapture();
		}

		private void OnWorldMapMouseMove(object sender, MouseEventArgs e)
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0064: Unknown result type (might be due to invalid IL or missing references)
			Point position = e.GetPosition(WorldMapImage);
			_viewModel.Exploration.UpdateHover((int)((Point)(ref position)).X, (int)((Point)(ref position)).Y);
			Point? mapDragStart = _mapDragStart;
			if (mapDragStart.HasValue)
			{
				Point valueOrDefault = mapDragStart.GetValueOrDefault();
				if (e.LeftButton == MouseButtonState.Pressed)
				{
					Point position2 = e.GetPosition(WorldMapScroll);
					WorldMapScroll.ScrollToHorizontalOffset(_mapDragStartH - (((Point)(ref position2)).X - ((Point)(ref valueOrDefault)).X));
					WorldMapScroll.ScrollToVerticalOffset(_mapDragStartV - (((Point)(ref position2)).Y - ((Point)(ref valueOrDefault)).Y));
					((DependencyObject)MapTooltipBorder).SetCurrentValue(UIElement.VisibilityProperty, (object)Visibility.Collapsed);
					return;
				}
			}
			PositionMapTooltip(e.GetPosition(MapTooltipCanvas));
		}

		private void PositionMapTooltip(Point cursorPos)
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			MapTooltipBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			Size desiredSize = MapTooltipBorder.DesiredSize;
			double num = ((Point)(ref cursorPos)).X + 16.0;
			if (num + ((Size)(ref desiredSize)).Width > MapTooltipCanvas.ActualWidth)
			{
				num = ((Point)(ref cursorPos)).X - 16.0 - ((Size)(ref desiredSize)).Width;
			}
			double num2 = ((Point)(ref cursorPos)).Y + 18.0;
			if (num2 + ((Size)(ref desiredSize)).Height > MapTooltipCanvas.ActualHeight)
			{
				num2 = ((Point)(ref cursorPos)).Y - 18.0 - ((Size)(ref desiredSize)).Height;
			}
			Canvas.SetLeft(MapTooltipBorder, Math.Max(0.0, num));
			Canvas.SetTop(MapTooltipBorder, Math.Max(0.0, num2));
			((DependencyObject)MapTooltipBorder).SetCurrentValue(UIElement.VisibilityProperty, (object)Visibility.Visible);
		}

		private void OnWorldMapMouseLeave(object sender, MouseEventArgs e)
		{
			_viewModel.Exploration.UpdateHover(-1, -1);
			((DependencyObject)MapTooltipBorder).SetCurrentValue(UIElement.VisibilityProperty, (object)Visibility.Collapsed);
		}

		private void OnNavigateToTile(int tileX, int tileY)
		{
			double zoom = _viewModel.Exploration.Zoom;
			WorldMapScroll.ScrollToHorizontalOffset((double)tileX * zoom - WorldMapScroll.ViewportWidth / 2.0);
			WorldMapScroll.ScrollToVerticalOffset((double)tileY * zoom - WorldMapScroll.ViewportHeight / 2.0);
		}

		private void OnLibraryCardMouseDown(object sender, MouseButtonEventArgs e)
		{
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			_dragStartLibrary = e.GetPosition(null);
		}

		private void OnLibraryCardMouseMove(object sender, MouseEventArgs e)
		{
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			if (e.LeftButton != MouseButtonState.Pressed)
			{
				return;
			}
			Point? dragStartLibrary = _dragStartLibrary;
			Point valueOrDefault = default(Point);
			int num;
			if (dragStartLibrary.HasValue)
			{
				valueOrDefault = dragStartLibrary.GetValueOrDefault();
				num = 1;
			}
			else
			{
				num = 0;
			}
			if (num == 0)
			{
				return;
			}
			Point position = e.GetPosition(null);
			if (!(Math.Abs(((Point)(ref position)).X - ((Point)(ref valueOrDefault)).X) < SystemParameters.MinimumHorizontalDragDistance) || !(Math.Abs(((Point)(ref position)).Y - ((Point)(ref valueOrDefault)).Y) < SystemParameters.MinimumVerticalDragDistance))
			{
				_dragStartLibrary = null;
				if (sender is FrameworkElement { DataContext: LibraryItemViewModel dataContext } frameworkElement)
				{
					DragDrop.DoDragDrop((DependencyObject)(object)frameworkElement, new DataObject(typeof(LibraryItemViewModel), dataContext), DragDropEffects.Copy);
				}
			}
		}

		private void OnItemSlotMouseDown(object sender, MouseButtonEventArgs e)
		{
			//IL_0004: Unknown result type (might be due to invalid IL or missing references)
			_dragStartSlot = e.GetPosition(null);
			if (sender is FrameworkElement { DataContext: ItemSlotViewModel dataContext })
			{
				_viewModel.SelectSlot(dataContext);
			}
		}

		private void OnItemSlotMouseMove(object sender, MouseEventArgs e)
		{
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			if (e.LeftButton != MouseButtonState.Pressed)
			{
				return;
			}
			Point? dragStartSlot = _dragStartSlot;
			Point valueOrDefault = default(Point);
			int num;
			if (dragStartSlot.HasValue)
			{
				valueOrDefault = dragStartSlot.GetValueOrDefault();
				num = 1;
			}
			else
			{
				num = 0;
			}
			if (num == 0)
			{
				return;
			}
			Point position = e.GetPosition(null);
			if (!(Math.Abs(((Point)(ref position)).X - ((Point)(ref valueOrDefault)).X) < SystemParameters.MinimumHorizontalDragDistance) || !(Math.Abs(((Point)(ref position)).Y - ((Point)(ref valueOrDefault)).Y) < SystemParameters.MinimumVerticalDragDistance))
			{
				_dragStartSlot = null;
				if (sender is FrameworkElement { DataContext: ItemSlotViewModel { IsEmpty: false } dataContext } frameworkElement)
				{
					DragDrop.DoDragDrop((DependencyObject)(object)frameworkElement, new DataObject(typeof(ItemSlotViewModel), dataContext), DragDropEffects.Move);
				}
			}
		}

		private void OnItemSlotDrop(object sender, DragEventArgs e)
		{
			if (sender is FrameworkElement { DataContext: ItemSlotViewModel dataContext })
			{
				if (e.Data.GetDataPresent(typeof(LibraryItemViewModel)) && e.Data.GetData(typeof(LibraryItemViewModel)) is LibraryItemViewModel libraryItemViewModel)
				{
					dataContext.PlaceItem(libraryItemViewModel.Id);
				}
				else if (e.Data.GetDataPresent(typeof(ItemSlotViewModel)) && e.Data.GetData(typeof(ItemSlotViewModel)) is ItemSlotViewModel itemSlotViewModel && itemSlotViewModel != dataContext)
				{
					itemSlotViewModel.SwapWith(dataContext);
				}
			}
		}

		[DebuggerNonUserCode]
		[GeneratedCode("PresentationBuildTasks", "10.0.11.0")]
		public void InitializeComponent()
		{
			if (!_contentLoaded)
			{
				_contentLoaded = true;
				Uri resourceLocator = new Uri("/TerrasavrNative.App;V1.2.0.0;component/mainwindow.xaml", UriKind.Relative);
				Application.LoadComponent(this, resourceLocator);
			}
		}

		[DebuggerNonUserCode]
		[GeneratedCode("PresentationBuildTasks", "10.0.11.0")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		void IComponentConnector.Connect(int connectionId, object target)
		{
			switch (connectionId)
			{
			case 2:
				((Button)target).Click += OnLoadClick;
				break;
			case 4:
				((Button)target).Click += OnLoadWorldClick;
				break;
			case 5:
				WorldMapScroll = (ScrollViewer)target;
				WorldMapScroll.PreviewMouseWheel += OnWorldMapPreviewMouseWheel;
				WorldMapScroll.PreviewMouseLeftButtonDown += OnWorldMapMouseDown;
				WorldMapScroll.PreviewMouseMove += OnWorldMapMouseMove;
				WorldMapScroll.PreviewMouseLeftButtonUp += OnWorldMapMouseUp;
				WorldMapScroll.MouseLeave += OnWorldMapMouseLeave;
				break;
			case 6:
				WorldMapImage = (Image)target;
				break;
			case 7:
				MapTooltipCanvas = (Canvas)target;
				break;
			case 8:
				MapTooltipBorder = (Border)target;
				break;
			default:
				_contentLoaded = true;
				break;
			}
		}

		[DebuggerNonUserCode]
		[GeneratedCode("PresentationBuildTasks", "10.0.11.0")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		void IStyleConnector.Connect(int connectionId, object target)
		{
			switch (connectionId)
			{
			case 1:
				((Border)target).PreviewMouseLeftButtonDown += OnItemSlotMouseDown;
				((Border)target).MouseMove += OnItemSlotMouseMove;
				((Border)target).Drop += OnItemSlotDrop;
				break;
			case 3:
				((Border)target).PreviewMouseLeftButtonDown += OnLibraryCardMouseDown;
				((Border)target).MouseMove += OnLibraryCardMouseMove;
				break;
			}
		}
	}
}
namespace TerrasavrNative.App.ViewModels
{
	public sealed class AboutViewModel
	{
		public string AppName => "Terrakeep";

		public string Tagline => "Editor de personajes de Terraria, nativo y sin Electron - vanilla y Calamity Mod.";

		public string Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0";

		public string CreditsText => "Terrakeep es una reescritura nativa (C#/.NET, WPF) de un editor de personajes de Terraria - sin Chromium ni Electron. El formato de archivo (.plr/.tplr, NBT, cifrado) se investigo y verifico de forma independiente, directamente contra el juego real y tModLoader, sin depender de codigo de terceros.\n\nInspirado en Terrasavr, de YellowAfterlife (yal.cc) - un editor excelente al que este proyecto debe la idea original. Terrakeep no es una copia ni un fork de ese codigo (el motor de YellowAfterlife esta compilado, nunca se tuvo acceso a su fuente): es un programa distinto, escrito desde cero, con su propia base de codigo.\n\nTerraria, tModLoader y Calamity Mod son propiedad de sus respectivos autores (Re-Logic, el equipo de tModLoader y el equipo de CalamityMod). Terrakeep no esta afiliado con ninguno de ellos.";
	}
	public class AppearanceViewModel : ObservableObject
	{
		private readonly CharacterFileService _service;

		private PlrCharacter? _character;

		private bool _suppressWriteback;

		[ObservableProperty]
		private WriteableBitmap? _previewImage;

		[ObservableProperty]
		private int _hairStyle;

		[ObservableProperty]
		private int _hairDye;

		[ObservableProperty]
		private string _hairDyeDisplayName = "Ninguno";

		[ObservableProperty]
		private bool _isMale = true;

		[ObservableProperty]
		private bool _isHairPickerOpen;

		[ObservableProperty]
		private bool _isHairDyePickerOpen;

		[ObservableProperty]
		private int _difficulty;

		[ObservableProperty]
		private int _healthNow;

		[ObservableProperty]
		private int _healthMax;

		[ObservableProperty]
		private int _manaNow;

		[ObservableProperty]
		private int _manaMax;

		[ObservableProperty]
		private int _fishingQuestsCompleted;

		[ObservableProperty]
		private int _golferScore;

		[ObservableProperty]
		private double _playHours;

		private const int HairIdx = 0;

		private const int SkinIdx = 1;

		private const int EyesIdx = 2;

		private const int ShirtIdx = 3;

		private const int UnderIdx = 4;

		private const int PantsIdx = 5;

		private const int ShoesIdx = 6;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand? openHairPickerCommand;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand<int>? selectHairCommand;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand? closeHairPickerCommand;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand? openHairDyePickerCommand;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand<int>? selectHairDyeCommand;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand? closeHairDyePickerCommand;

		public bool IsFemale
		{
			get
			{
				return !IsMale;
			}
			set
			{
				IsMale = !value;
			}
		}

		public ObservableCollection<ColorSwatchViewModel> Swatches { get; } = new ObservableCollection<ColorSwatchViewModel>();

		public ObservableCollection<HairOptionViewModel> HairOptions { get; } = new ObservableCollection<HairOptionViewModel>();

		public ObservableCollection<HairDyeOptionViewModel> HairDyeOptions { get; } = new ObservableCollection<HairDyeOptionViewModel>();

		public string[] DifficultyLabels { get; } = new string[4] { "Softcore", "Mediumcore", "Hardcore", "Journey" };

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public WriteableBitmap? PreviewImage
		{
			get
			{
				return _previewImage;
			}
			set
			{
				if (!EqualityComparer<WriteableBitmap>.Default.Equals(_previewImage, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PreviewImage);
					_previewImage = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PreviewImage);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public int HairStyle
		{
			get
			{
				return _hairStyle;
			}
			set
			{
				if (!EqualityComparer<int>.Default.Equals(_hairStyle, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HairStyle);
					_hairStyle = value;
					OnHairStyleChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HairStyle);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public int HairDye
		{
			get
			{
				return _hairDye;
			}
			set
			{
				if (!EqualityComparer<int>.Default.Equals(_hairDye, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HairDye);
					_hairDye = value;
					OnHairDyeChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HairDye);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public string HairDyeDisplayName
		{
			get
			{
				return _hairDyeDisplayName;
			}
			[MemberNotNull("_hairDyeDisplayName")]
			set
			{
				if (!EqualityComparer<string>.Default.Equals(_hairDyeDisplayName, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HairDyeDisplayName);
					_hairDyeDisplayName = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HairDyeDisplayName);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool IsMale
		{
			get
			{
				return _isMale;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_isMale, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsMale);
					_isMale = value;
					OnIsMaleChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsMale);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool IsHairPickerOpen
		{
			get
			{
				return _isHairPickerOpen;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_isHairPickerOpen, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsHairPickerOpen);
					_isHairPickerOpen = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsHairPickerOpen);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool IsHairDyePickerOpen
		{
			get
			{
				return _isHairDyePickerOpen;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_isHairDyePickerOpen, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsHairDyePickerOpen);
					_isHairDyePickerOpen = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsHairDyePickerOpen);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public int Difficulty
		{
			get
			{
				return _difficulty;
			}
			set
			{
				if (!EqualityComparer<int>.Default.Equals(_difficulty, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Difficulty);
					_difficulty = value;
					OnDifficultyChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Difficulty);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public int HealthNow
		{
			get
			{
				return _healthNow;
			}
			set
			{
				if (!EqualityComparer<int>.Default.Equals(_healthNow, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HealthNow);
					_healthNow = value;
					OnHealthNowChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HealthNow);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public int HealthMax
		{
			get
			{
				return _healthMax;
			}
			set
			{
				if (!EqualityComparer<int>.Default.Equals(_healthMax, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HealthMax);
					_healthMax = value;
					OnHealthMaxChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HealthMax);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public int ManaNow
		{
			get
			{
				return _manaNow;
			}
			set
			{
				if (!EqualityComparer<int>.Default.Equals(_manaNow, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ManaNow);
					_manaNow = value;
					OnManaNowChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ManaNow);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public int ManaMax
		{
			get
			{
				return _manaMax;
			}
			set
			{
				if (!EqualityComparer<int>.Default.Equals(_manaMax, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ManaMax);
					_manaMax = value;
					OnManaMaxChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ManaMax);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public int FishingQuestsCompleted
		{
			get
			{
				return _fishingQuestsCompleted;
			}
			set
			{
				if (!EqualityComparer<int>.Default.Equals(_fishingQuestsCompleted, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.FishingQuestsCompleted);
					_fishingQuestsCompleted = value;
					OnFishingQuestsCompletedChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.FishingQuestsCompleted);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public int GolferScore
		{
			get
			{
				return _golferScore;
			}
			set
			{
				if (!EqualityComparer<int>.Default.Equals(_golferScore, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.GolferScore);
					_golferScore = value;
					OnGolferScoreChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.GolferScore);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public double PlayHours
		{
			get
			{
				return _playHours;
			}
			set
			{
				if (!EqualityComparer<double>.Default.Equals(_playHours, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PlayHours);
					_playHours = value;
					OnPlayHoursChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PlayHours);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand OpenHairPickerCommand => openHairPickerCommand ?? (openHairPickerCommand = new RelayCommand(OpenHairPicker));

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand<int> SelectHairCommand => selectHairCommand ?? (selectHairCommand = new RelayCommand<int>(SelectHair));

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand CloseHairPickerCommand => closeHairPickerCommand ?? (closeHairPickerCommand = new RelayCommand(CloseHairPicker));

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand OpenHairDyePickerCommand => openHairDyePickerCommand ?? (openHairDyePickerCommand = new RelayCommand(OpenHairDyePicker));

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand<int> SelectHairDyeCommand => selectHairDyeCommand ?? (selectHairDyeCommand = new RelayCommand<int>(SelectHairDye));

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand CloseHairDyePickerCommand => closeHairDyePickerCommand ?? (closeHairDyePickerCommand = new RelayCommand(CloseHairDyePicker));

		public AppearanceViewModel(CharacterFileService service)
		{
			_service = service;
			BuildHairDyeOptions();
		}

		[RelayCommand]
		private void OpenHairPicker()
		{
			if (HairOptions.Count == 0 && Swatches.Count > 0)
			{
				PlayerPreviewRenderer.Tint hairColor = new PlayerPreviewRenderer.Tint((byte)Swatches[0].R, (byte)Swatches[0].G, (byte)Swatches[0].B);
				for (int i = 1; i <= PlayerPreviewRenderer.HairStyleCount; i++)
				{
					HairOptions.Add(new HairOptionViewModel(i, PlayerPreviewRenderer.RenderHairThumbnail(i, hairColor)));
				}
			}
			IsHairPickerOpen = true;
		}

		[RelayCommand]
		private void SelectHair(int id)
		{
			HairStyle = id;
			IsHairPickerOpen = false;
		}

		[RelayCommand]
		private void CloseHairPicker()
		{
			IsHairPickerOpen = false;
		}

		private void BuildHairDyeOptions()
		{
			HairDyeOptions.Add(new HairDyeOptionViewModel(0, "Ninguno", null));
			foreach (HairDyeEntryData entry in _service.HairDyes.Entries)
			{
				string name = _service.VanillaCatalog.GetName(entry.ItemId);
				string iconPath = VanillaIconResolver.GetIconPath(entry.ItemId);
				HairDyeOptions.Add(new HairDyeOptionViewModel(entry.Index, name, iconPath));
			}
		}

		[RelayCommand]
		private void OpenHairDyePicker()
		{
			IsHairDyePickerOpen = true;
		}

		[RelayCommand]
		private void SelectHairDye(int index)
		{
			HairDye = index;
			IsHairDyePickerOpen = false;
		}

		[RelayCommand]
		private void CloseHairDyePicker()
		{
			IsHairDyePickerOpen = false;
		}

		public void LoadFrom(PlrCharacter character)
		{
			_character = null;
			Swatches.Clear();
			Swatches.Add(new ColorSwatchViewModel("Pelo", character.HairColor));
			Swatches.Add(new ColorSwatchViewModel("Piel", character.SkinColor));
			Swatches.Add(new ColorSwatchViewModel("Ojos", character.EyeColor));
			Swatches.Add(new ColorSwatchViewModel("Camisa", character.ShirtColor));
			Swatches.Add(new ColorSwatchViewModel("Camiseta interior", character.UnderColor));
			Swatches.Add(new ColorSwatchViewModel("Pantalones", character.PantsColor));
			Swatches.Add(new ColorSwatchViewModel("Zapatos", character.ShoesColor));
			_suppressWriteback = true;
			HairStyle = character.HairStyle;
			HairDye = character.HairDye;
			IsMale = character.Gender == 1;
			Difficulty = character.Difficulty;
			HealthNow = character.HealthNow;
			HealthMax = character.HealthMax;
			ManaNow = character.ManaNow;
			ManaMax = character.ManaMax;
			FishingQuestsCompleted = character.FishingQuestsCompleted;
			GolferScore = character.GolferScore;
			long value = (long)(((ulong)character.PlayTimeHigh << 32) | character.PlayTimeLow);
			PlayHours = TimeSpan.FromTicks(value).TotalHours;
			_suppressWriteback = false;
			for (int i = 0; i < Swatches.Count; i++)
			{
				ColorSwatchViewModel colorSwatchViewModel = Swatches[i];
				colorSwatchViewModel.PropertyChanged += delegate
				{
					RefreshPreview();
				};
				if (i == 0)
				{
					colorSwatchViewModel.PropertyChanged += delegate
					{
						HairOptions.Clear();
					};
				}
			}
			HairOptions.Clear();
			_character = character;
			RefreshPreview();
		}

		private void RefreshPreview()
		{
			if (Swatches.Count >= 7)
			{
				PreviewImage = PlayerPreviewRenderer.Render(colors: new PlayerPreviewRenderer.PlayerColors(T(0), T(1), T(2), T(3), T(4), T(5), T(6)), hairStyle: HairStyle, isMale: IsMale);
			}
			PlayerPreviewRenderer.Tint T(int i)
			{
				return new PlayerPreviewRenderer.Tint((byte)Swatches[i].R, (byte)Swatches[i].G, (byte)Swatches[i].B);
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnHairStyleChanged(int value)
		{
			RefreshPreview();
			if (!_suppressWriteback && _character != null)
			{
				_character.HairStyle = value;
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnHairDyeChanged(int value)
		{
			HairDyeDisplayName = HairDyeOptions.FirstOrDefault((HairDyeOptionViewModel o) => o.Index == value)?.DisplayName ?? $"Tinte #{value}";
			if (!_suppressWriteback && _character != null)
			{
				_character.HairDye = (byte)Math.Clamp(value, 0, 255);
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnIsMaleChanged(bool value)
		{
			OnPropertyChanged("IsFemale");
			RefreshPreview();
			if (!_suppressWriteback && _character != null)
			{
				_character.Gender = (byte)(value ? 1u : 0u);
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnDifficultyChanged(int value)
		{
			if (!_suppressWriteback && _character != null)
			{
				_character.Difficulty = (byte)Math.Clamp(value, 0, 3);
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnHealthNowChanged(int value)
		{
			if (!_suppressWriteback && _character != null)
			{
				_character.HealthNow = value;
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnHealthMaxChanged(int value)
		{
			if (!_suppressWriteback && _character != null)
			{
				_character.HealthMax = value;
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnManaNowChanged(int value)
		{
			if (!_suppressWriteback && _character != null)
			{
				_character.ManaNow = value;
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnManaMaxChanged(int value)
		{
			if (!_suppressWriteback && _character != null)
			{
				_character.ManaMax = value;
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnFishingQuestsCompletedChanged(int value)
		{
			if (!_suppressWriteback && _character != null)
			{
				_character.FishingQuestsCompleted = value;
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnGolferScoreChanged(int value)
		{
			if (!_suppressWriteback && _character != null)
			{
				_character.GolferScore = value;
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnPlayHoursChanged(double value)
		{
			if (!_suppressWriteback && _character != null)
			{
				long ticks = TimeSpan.FromHours(Math.Max(0.0, value)).Ticks;
				_character.PlayTimeLow = (uint)ticks;
				_character.PlayTimeHigh = (uint)(ticks >> 32);
			}
		}
	}
	public sealed class BuffCatalogEntryViewModel(string displayName, int id, bool isCalamity, string? iconPath, string? description)
	{
		public string DisplayName { get; } = displayName;

		public int Id { get; } = id;

		public bool IsCalamity { get; } = isCalamity;

		public string? IconPath { get; } = iconPath;

		public string? Description { get; } = description;
	}
	public class BuffRowViewModel : ObservableObject
	{
		private readonly Action<BuffRowViewModel> _requestRemove;

		private bool _suppressWriteback;

		[ObservableProperty]
		private int _durationSeconds;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand? removeCommand;

		public PlrBuff Buff { get; }

		public string Name { get; }

		public bool IsCalamity { get; }

		public string? IconPath { get; }

		public string? Description { get; }

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public int DurationSeconds
		{
			get
			{
				return _durationSeconds;
			}
			set
			{
				if (!EqualityComparer<int>.Default.Equals(_durationSeconds, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.DurationSeconds);
					_durationSeconds = value;
					OnDurationSecondsChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.DurationSeconds);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand RemoveCommand => removeCommand ?? (removeCommand = new RelayCommand(Remove));

		public BuffRowViewModel(PlrBuff buff, string name, bool isCalamity, string? iconPath, string? description, Action<BuffRowViewModel> requestRemove)
		{
			Buff = buff;
			Name = name;
			IsCalamity = isCalamity;
			IconPath = iconPath;
			Description = description;
			_requestRemove = requestRemove;
			_suppressWriteback = true;
			DurationSeconds = buff.Time / 60;
			_suppressWriteback = false;
		}

		[RelayCommand]
		private void Remove()
		{
			_requestRemove(this);
		}

		public static BuffRowViewModel From(PlrBuff buff, VanillaBuffCatalog vanillaCatalog, CalamityBuffCatalog calamityCatalog, Action<BuffRowViewModel> requestRemove)
		{
			bool flag = buff.Id >= 25000000;
			string name;
			string iconPath;
			string description;
			if (flag)
			{
				CalamityBuffEntry calamityBuffEntry = calamityCatalog.BySyntheticId(buff.Id);
				name = calamityBuffEntry?.DisplayName ?? $"Calamity #{buff.Id}";
				iconPath = ((calamityBuffEntry != null && calamityBuffEntry.Icon != null) ? ("pack://siteoforigin:,,,/Assets/calamity/buff_icons/" + calamityBuffEntry.Icon) : null);
				description = calamityBuffEntry?.Description;
			}
			else
			{
				name = vanillaCatalog.GetName(buff.Id);
				iconPath = VanillaBuffIconResolver.GetIconPath(buff.Id);
				description = vanillaCatalog.GetDescription(buff.Id);
			}
			return new BuffRowViewModel(buff, name, flag, iconPath, description, requestRemove);
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnDurationSecondsChanged(int value)
		{
			if (!_suppressWriteback)
			{
				Buff.Time = Math.Max(0, value) * 60;
			}
		}
	}
	public class BuffsViewModel : ObservableObject
	{
		private const int MaxResults = 200;

		private const int DefaultDurationSeconds = 600;

		private readonly VanillaBuffCatalog _vanillaCatalog;

		private readonly CalamityBuffCatalog _calamityCatalog;

		private readonly List<BuffCatalogEntryViewModel> _all;

		private PlrCharacter? _character;

		[ObservableProperty]
		private string _searchText = string.Empty;

		[ObservableProperty]
		private string _resultsSummary = string.Empty;

		[ObservableProperty]
		private bool _isPicking;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand? beginAddCommand;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand? cancelAddCommand;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand<BuffCatalogEntryViewModel?>? pickBuffCommand;

		public ObservableCollection<BuffRowViewModel> Active { get; } = new ObservableCollection<BuffRowViewModel>();

		public ObservableCollection<BuffCatalogEntryViewModel> Results { get; } = new ObservableCollection<BuffCatalogEntryViewModel>();

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public string SearchText
		{
			get
			{
				return _searchText;
			}
			[MemberNotNull("_searchText")]
			set
			{
				if (!EqualityComparer<string>.Default.Equals(_searchText, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SearchText);
					_searchText = value;
					OnSearchTextChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SearchText);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public string ResultsSummary
		{
			get
			{
				return _resultsSummary;
			}
			[MemberNotNull("_resultsSummary")]
			set
			{
				if (!EqualityComparer<string>.Default.Equals(_resultsSummary, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ResultsSummary);
					_resultsSummary = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ResultsSummary);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool IsPicking
		{
			get
			{
				return _isPicking;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_isPicking, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsPicking);
					_isPicking = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsPicking);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand BeginAddCommand => beginAddCommand ?? (beginAddCommand = new RelayCommand(BeginAdd));

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand CancelAddCommand => cancelAddCommand ?? (cancelAddCommand = new RelayCommand(CancelAdd));

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand<BuffCatalogEntryViewModel?> PickBuffCommand => pickBuffCommand ?? (pickBuffCommand = new RelayCommand<BuffCatalogEntryViewModel>(PickBuff));

		public BuffsViewModel(CharacterFileService service)
		{
			_vanillaCatalog = service.VanillaBuffs;
			_calamityCatalog = service.CalamityBuffCatalog;
			_all = new List<BuffCatalogEntryViewModel>();
			foreach (var (num, displayName) in _vanillaCatalog.AllEntries())
			{
				_all.Add(new BuffCatalogEntryViewModel(displayName, num, isCalamity: false, VanillaBuffIconResolver.GetIconPath(num), _vanillaCatalog.GetDescription(num)));
			}
			foreach (CalamityBuffEntry entry in _calamityCatalog.Entries)
			{
				string iconPath = ((entry.Icon != null) ? ("pack://siteoforigin:,,,/Assets/calamity/buff_icons/" + entry.Icon) : null);
				_all.Add(new BuffCatalogEntryViewModel(entry.DisplayName, entry.SyntheticId, isCalamity: true, iconPath, entry.Description));
			}
		}

		public void LoadFrom(PlrCharacter character)
		{
			_character = character;
			Active.Clear();
			IsPicking = false;
			SearchText = string.Empty;
			foreach (PlrBuff buff in character.Buffs)
			{
				if (buff.Id != 0)
				{
					Active.Add(BuffRowViewModel.From(buff, _vanillaCatalog, _calamityCatalog, RemoveRow));
				}
			}
		}

		private void RemoveRow(BuffRowViewModel row)
		{
			row.Buff.Id = 0;
			row.Buff.Time = 0;
			Active.Remove(row);
		}

		[RelayCommand]
		private void BeginAdd()
		{
			IsPicking = true;
			SearchText = string.Empty;
			ApplyFilter();
		}

		[RelayCommand]
		private void CancelAdd()
		{
			IsPicking = false;
		}

		private void ApplyFilter()
		{
			Results.Clear();
			List<BuffCatalogEntryViewModel> list = (string.IsNullOrWhiteSpace(SearchText) ? _all : _all.Where((BuffCatalogEntryViewModel b) => b.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList());
			foreach (BuffCatalogEntryViewModel item in list.Take(200))
			{
				Results.Add(item);
			}
			ResultsSummary = ((list.Count > 200) ? $"Mostrando {200} de {list.Count} - {(string.IsNullOrWhiteSpace(SearchText) ? "escribe para afinar la busqueda." : "afina la busqueda.")}" : $"{list.Count} resultado(s).");
		}

		[RelayCommand]
		private void PickBuff(BuffCatalogEntryViewModel? entry)
		{
			if (_character != null && entry != null)
			{
				PlrBuff plrBuff = _character.Buffs.FirstOrDefault((PlrBuff b) => b.Id == 0);
				if (plrBuff != null)
				{
					plrBuff.Id = entry.Id;
					plrBuff.Time = 36000;
					Active.Add(BuffRowViewModel.From(plrBuff, _vanillaCatalog, _calamityCatalog, RemoveRow));
					IsPicking = false;
					SearchText = string.Empty;
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnSearchTextChanged(string value)
		{
			ApplyFilter();
		}
	}
	public sealed class BuildClassGearViewModel(string className, List<BuildItemRowViewModel> armor, List<BuildItemRowViewModel> weapons, List<BuildItemRowViewModel> accessories, BuildClassGear source)
	{
		public string ClassName { get; } = className;

		public List<BuildItemRowViewModel> Armor { get; } = armor;

		public List<BuildItemRowViewModel> Weapons { get; } = weapons;

		public List<BuildItemRowViewModel> Accessories { get; } = accessories;

		public BuildClassGear Source { get; } = source;
	}
	public sealed class BuildItemRowViewModel(string displayName, string? prefixText, string? iconPath, bool isCalamity, string? statsTooltip)
	{
		public string DisplayName { get; } = displayName;

		public string? PrefixText { get; } = prefixText;

		public string? IconPath { get; } = iconPath;

		public bool IsCalamity { get; } = isCalamity;

		public string? StatsTooltip { get; } = statsTooltip;
	}
	public sealed class BuildStageViewModel(string label, List<BuildClassGearViewModel> classes)
	{
		public string Label { get; } = label;

		public List<BuildClassGearViewModel> Classes { get; } = classes;
	}
	public sealed class BuildsViewModel
	{
		public IReadOnlyList<BuildStageViewModel> VanillaStages { get; }

		public IReadOnlyList<BuildStageViewModel> CalamityStages { get; }

		public BuildsViewModel(BuildsCatalog vanilla, BuildsCatalog calamity, CharacterFileService service)
		{
			VanillaStages = vanilla.Stages.Select((BuildStage s) => ResolveStage(s, service)).ToList();
			CalamityStages = calamity.Stages.Select((BuildStage s) => ResolveStage(s, service)).ToList();
		}

		private static BuildStageViewModel ResolveStage(BuildStage stage, CharacterFileService service)
		{
			List<BuildClassGearViewModel> classes = stage.Classes.Select<KeyValuePair<string, BuildClassGear>, BuildClassGearViewModel>((KeyValuePair<string, BuildClassGear> kv) => ResolveClass(kv.Key, kv.Value, service)).ToList();
			return new BuildStageViewModel(stage.Label, classes);
		}

		private static BuildClassGearViewModel ResolveClass(string className, BuildClassGear gear, CharacterFileService service)
		{
			return new BuildClassGearViewModel(className, gear.Armor.Select((BuildItemRef i) => ResolveItem(i, service)).ToList(), gear.Weapons.Select((BuildItemRef i) => ResolveItem(i, service)).ToList(), gear.Accessories.Select((BuildItemRef i) => ResolveItem(i, service)).ToList(), gear);
		}

		private static BuildItemRowViewModel ResolveItem(BuildItemRef itemRef, CharacterFileService service)
		{
			string iconPath = null;
			string statsTooltip = null;
			bool isCalamity = false;
			if (!string.IsNullOrEmpty(itemRef.Pid))
			{
				int num = itemRef.Pid.IndexOf('/');
				if (num >= 0)
				{
					isCalamity = true;
					CalamityCatalogEntry calamityCatalogEntry = service.CalamityCatalog.ByModAndInternal(itemRef.Pid.Substring(0, num), itemRef.Pid.Substring(num + 1));
					if (calamityCatalogEntry?.Icon != null)
					{
						iconPath = "pack://siteoforigin:,,,/Assets/calamity/icons/" + calamityCatalogEntry.Icon;
					}
					if (calamityCatalogEntry != null)
					{
						statsTooltip = ItemStatsFormatter.Format(isCalamity: true, calamityCatalogEntry.SyntheticId, service.VanillaStats, service.CalamityCatalog, service.VanillaCategories);
					}
				}
				else
				{
					int? idByKey = service.VanillaCatalog.GetIdByKey(itemRef.Pid);
					if (idByKey.HasValue)
					{
						iconPath = VanillaIconResolver.GetIconPath(idByKey.Value);
						statsTooltip = ItemStatsFormatter.Format(isCalamity: false, idByKey.Value, service.VanillaStats, service.CalamityCatalog, service.VanillaCategories);
					}
				}
			}
			return new BuildItemRowViewModel(itemRef.DisplayName, itemRef.Prefix, iconPath, isCalamity, statsTooltip);
		}
	}
	public sealed class CategoryNodeViewModel(string name, string fullPath) : ObservableObject
	{
		[ObservableProperty]
		private int _itemCount;

		[ObservableProperty]
		private string? _iconPath;

		[ObservableProperty]
		private bool _isSelected;

		[ObservableProperty]
		private bool _isExpanded;

		public string Name { get; } = name;

		public string FullPath { get; } = fullPath;

		public ObservableCollection<CategoryNodeViewModel> Children { get; } = new ObservableCollection<CategoryNodeViewModel>();

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public int ItemCount
		{
			get
			{
				return _itemCount;
			}
			set
			{
				if (!EqualityComparer<int>.Default.Equals(_itemCount, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ItemCount);
					_itemCount = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ItemCount);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public string? IconPath
		{
			get
			{
				return _iconPath;
			}
			set
			{
				if (!EqualityComparer<string>.Default.Equals(_iconPath, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IconPath);
					_iconPath = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IconPath);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool IsSelected
		{
			get
			{
				return _isSelected;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_isSelected, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsSelected);
					_isSelected = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsSelected);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool IsExpanded
		{
			get
			{
				return _isExpanded;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_isExpanded, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsExpanded);
					_isExpanded = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsExpanded);
				}
			}
		}
	}
	public sealed class ChangelogViewModel(ChangelogCatalog catalog)
	{
		public IReadOnlyList<ChangelogEntry> Entries { get; } = catalog.Entries;
	}
	public class ColorSwatchViewModel : ObservableObject
	{
		private readonly byte[] _target;

		private bool _suppressWriteback;

		[ObservableProperty]
		private int _r;

		[ObservableProperty]
		private int _g;

		[ObservableProperty]
		private int _b;

		[ObservableProperty]
		private Brush _preview = Brushes.Black;

		public string Label { get; }

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public int R
		{
			get
			{
				return _r;
			}
			set
			{
				if (!EqualityComparer<int>.Default.Equals(_r, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.R);
					_r = value;
					OnRChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.R);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public int G
		{
			get
			{
				return _g;
			}
			set
			{
				if (!EqualityComparer<int>.Default.Equals(_g, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.G);
					_g = value;
					OnGChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.G);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public int B
		{
			get
			{
				return _b;
			}
			set
			{
				if (!EqualityComparer<int>.Default.Equals(_b, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.B);
					_b = value;
					OnBChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.B);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public Brush Preview
		{
			get
			{
				return _preview;
			}
			[MemberNotNull("_preview")]
			set
			{
				if (!EqualityComparer<Brush>.Default.Equals(_preview, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Preview);
					_preview = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Preview);
				}
			}
		}

		public ColorSwatchViewModel(string label, byte[] target)
		{
			Label = label;
			_target = target;
			LoadFromTarget();
		}

		public void LoadFromTarget()
		{
			_suppressWriteback = true;
			R = ((_target.Length != 0) ? _target[0] : 0);
			G = ((_target.Length > 1) ? _target[1] : 0);
			B = ((_target.Length > 2) ? _target[2] : 0);
			_suppressWriteback = false;
			UpdatePreview();
		}

		private void WriteBack()
		{
			if (!_suppressWriteback)
			{
				if (_target.Length != 0)
				{
					_target[0] = (byte)Math.Clamp(R, 0, 255);
				}
				if (_target.Length > 1)
				{
					_target[1] = (byte)Math.Clamp(G, 0, 255);
				}
				if (_target.Length > 2)
				{
					_target[2] = (byte)Math.Clamp(B, 0, 255);
				}
				UpdatePreview();
			}
		}

		private void UpdatePreview()
		{
			Preview = new SolidColorBrush(Color.FromRgb((byte)Math.Clamp(R, 0, 255), (byte)Math.Clamp(G, 0, 255), (byte)Math.Clamp(B, 0, 255)));
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnRChanged(int value)
		{
			WriteBack();
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnGChanged(int value)
		{
			WriteBack();
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnBChanged(int value)
		{
			WriteBack();
		}
	}
	public sealed class ContainerViewModel(string key, string displayName, ObservableCollection<ItemSlotViewModel> slots)
	{
		public string Key { get; } = key;

		public string DisplayName { get; } = displayName;

		public ObservableCollection<ItemSlotViewModel> Slots { get; } = slots;
	}
	public sealed class EquipmentOptionViewModel(string label, int value) : ObservableObject
	{
		[ObservableProperty]
		private bool _isSelected;

		public string Label { get; } = label;

		public int Value { get; } = value;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool IsSelected
		{
			get
			{
				return _isSelected;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_isSelected, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsSelected);
					_isSelected = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsSelected);
				}
			}
		}
	}
	public enum EquipmentKind
	{
		Items,
		Social,
		Dyes
	}
	public class EquipmentGroupViewModel : ObservableObject
	{
		private readonly Dictionary<(int Loadout, EquipmentKind Kind), ContainerViewModel> _byKey = new Dictionary<(int, EquipmentKind), ContainerViewModel>();

		[ObservableProperty]
		private int _selectedLoadout;

		[ObservableProperty]
		private EquipmentKind _selectedKind = EquipmentKind.Items;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand<EquipmentOptionViewModel?>? selectLoadoutCommand;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand<EquipmentOptionViewModel?>? selectKindCommand;

		public IReadOnlyList<ContainerViewModel> AllContainers { get; }

		public ContainerViewModel EquippedItems => _byKey[(0, EquipmentKind.Items)];

		public ObservableCollection<EquipmentOptionViewModel> LoadoutOptions { get; } = new ObservableCollection<EquipmentOptionViewModel>();

		public ObservableCollection<EquipmentOptionViewModel> KindOptions { get; } = new ObservableCollection<EquipmentOptionViewModel>
		{
			new EquipmentOptionViewModel("Armadura", 0)
			{
				IsSelected = true
			},
			new EquipmentOptionViewModel("Vanidad", 1),
			new EquipmentOptionViewModel("Tintes", 2)
		};

		public ObservableCollection<ItemSlotViewModel> CurrentSlots => _byKey[(SelectedLoadout, SelectedKind)].Slots;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public int SelectedLoadout
		{
			get
			{
				return _selectedLoadout;
			}
			set
			{
				if (!EqualityComparer<int>.Default.Equals(_selectedLoadout, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedLoadout);
					_selectedLoadout = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedLoadout);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public EquipmentKind SelectedKind
		{
			get
			{
				return _selectedKind;
			}
			set
			{
				if (!EqualityComparer<EquipmentKind>.Default.Equals(_selectedKind, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedKind);
					_selectedKind = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedKind);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand<EquipmentOptionViewModel?> SelectLoadoutCommand => selectLoadoutCommand ?? (selectLoadoutCommand = new RelayCommand<EquipmentOptionViewModel>(SelectLoadout));

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand<EquipmentOptionViewModel?> SelectKindCommand => selectKindCommand ?? (selectKindCommand = new RelayCommand<EquipmentOptionViewModel>(SelectKind));

		public EquipmentGroupViewModel(CharacterFileService service, Action<ItemSlotViewModel> requestPickForSlot, Dictionary<string, GameItem[]> mergedContainers, int realLoadoutCount)
		{
			AddSlotSet(service, requestPickForSlot, 0, EquipmentKind.Items, "Equipo puesto - armadura/accesorios", mergedContainers["loadout0Items"]);
			AddSlotSet(service, requestPickForSlot, 0, EquipmentKind.Social, "Equipo puesto - vanidad", mergedContainers["loadout0Social"]);
			AddSlotSet(service, requestPickForSlot, 0, EquipmentKind.Dyes, "Equipo puesto - tintes", mergedContainers["loadout0Dyes"]);
			LoadoutOptions.Add(new EquipmentOptionViewModel("Puesto", 0)
			{
				IsSelected = true
			});
			for (int i = 1; i <= realLoadoutCount; i++)
			{
				AddSlotSet(service, requestPickForSlot, i, EquipmentKind.Items, $"Loadout {i} - armadura/accesorios", mergedContainers[$"loadout{i}Items"]);
				AddSlotSet(service, requestPickForSlot, i, EquipmentKind.Social, $"Loadout {i} - vanidad", mergedContainers[$"loadout{i}Social"]);
				AddSlotSet(service, requestPickForSlot, i, EquipmentKind.Dyes, $"Loadout {i} - tintes", mergedContainers[$"loadout{i}Dyes"]);
				LoadoutOptions.Add(new EquipmentOptionViewModel(i.ToString(), i));
			}
			AllContainers = _byKey.Values.ToList();
		}

		private void AddSlotSet(CharacterFileService service, Action<ItemSlotViewModel> requestPickForSlot, int loadout, EquipmentKind kind, string displayName, GameItem[] items)
		{
			ObservableCollection<ItemSlotViewModel> observableCollection = new ObservableCollection<ItemSlotViewModel>();
			for (int i = 0; i < items.Length; i++)
			{
				observableCollection.Add(new ItemSlotViewModel(service, i, displayName, items[i], requestPickForSlot));
			}
			if (1 == 0)
			{
			}
			string text = kind switch
			{
				EquipmentKind.Items => $"loadout{loadout}Items", 
				EquipmentKind.Social => $"loadout{loadout}Social", 
				EquipmentKind.Dyes => $"loadout{loadout}Dyes", 
				_ => throw new ArgumentOutOfRangeException("kind"), 
			};
			if (1 == 0)
			{
			}
			string key = text;
			_byKey[(loadout, kind)] = new ContainerViewModel(key, displayName, observableCollection);
		}

		[RelayCommand]
		private void SelectLoadout(EquipmentOptionViewModel? option)
		{
			if (option == null)
			{
				return;
			}
			SelectedLoadout = option.Value;
			foreach (EquipmentOptionViewModel loadoutOption in LoadoutOptions)
			{
				loadoutOption.IsSelected = loadoutOption == option;
			}
			OnPropertyChanged("CurrentSlots");
		}

		[RelayCommand]
		private void SelectKind(EquipmentOptionViewModel? option)
		{
			if (option == null)
			{
				return;
			}
			SelectedKind = (EquipmentKind)option.Value;
			foreach (EquipmentOptionViewModel kindOption in KindOptions)
			{
				kindOption.IsSelected = kindOption == option;
			}
			OnPropertyChanged("CurrentSlots");
		}
	}
	public sealed class WorldNpcRowViewModel(int id, string name, int x, int y, bool homeless)
	{
		public int Id { get; } = id;

		public string Name { get; } = name;

		public int TileX { get; } = x;

		public int TileY { get; } = y;

		public string Position { get; } = homeless ? $"({x}, {y}) - sin casa" : $"({x}, {y})";

		public string? IconPath { get; } = NpcIconResolver.GetIconPath(id);
	}
	public sealed class MissingNpcRowViewModel(int id, string name)
	{
		public string Name { get; } = name;

		public string? IconPath { get; } = NpcIconResolver.GetIconPath(id);
	}
	public class ExplorationViewModel : ObservableObject
	{
		private readonly NpcNameCatalog _npcNames;

		private readonly MapColorCatalog _mapColors;

		private readonly TileNameCatalog _tileNames;

		private List<WorldNpcRowViewModel> _allNpcs = new List<WorldNpcRowViewModel>();

		private WldWorld? _world;

		[ObservableProperty]
		private BitmapSource? _worldImage;

		[ObservableProperty]
		private string _statusMessage = "Sin mundo cargado.";

		[ObservableProperty]
		private string? _worldTitle;

		[ObservableProperty]
		private bool _isWorldLoaded;

		[ObservableProperty]
		private string _npcSearchText = string.Empty;

		[ObservableProperty]
		private double _zoom = 1.0;

		[ObservableProperty]
		private string _hoverInfo = string.Empty;

		private const double MinZoom = 0.1;

		private const double MaxZoom = 6.0;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand<WorldNpcRowViewModel>? goToNpcCommand;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand? zoomInCommand;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand? zoomOutCommand;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand? zoomResetCommand;

		public ObservableCollection<WorldNpcRowViewModel> Npcs { get; } = new ObservableCollection<WorldNpcRowViewModel>();

		public ObservableCollection<MissingNpcRowViewModel> MissingNpcs { get; } = new ObservableCollection<MissingNpcRowViewModel>();

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public BitmapSource? WorldImage
		{
			get
			{
				return _worldImage;
			}
			set
			{
				if (!EqualityComparer<BitmapSource>.Default.Equals(_worldImage, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.WorldImage);
					_worldImage = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.WorldImage);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public string StatusMessage
		{
			get
			{
				return _statusMessage;
			}
			[MemberNotNull("_statusMessage")]
			set
			{
				if (!EqualityComparer<string>.Default.Equals(_statusMessage, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.StatusMessage);
					_statusMessage = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.StatusMessage);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public string? WorldTitle
		{
			get
			{
				return _worldTitle;
			}
			set
			{
				if (!EqualityComparer<string>.Default.Equals(_worldTitle, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.WorldTitle);
					_worldTitle = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.WorldTitle);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool IsWorldLoaded
		{
			get
			{
				return _isWorldLoaded;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_isWorldLoaded, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsWorldLoaded);
					_isWorldLoaded = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsWorldLoaded);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public string NpcSearchText
		{
			get
			{
				return _npcSearchText;
			}
			[MemberNotNull("_npcSearchText")]
			set
			{
				if (!EqualityComparer<string>.Default.Equals(_npcSearchText, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.NpcSearchText);
					_npcSearchText = value;
					OnNpcSearchTextChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.NpcSearchText);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public double Zoom
		{
			get
			{
				return _zoom;
			}
			set
			{
				if (!EqualityComparer<double>.Default.Equals(_zoom, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Zoom);
					_zoom = value;
					OnZoomChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Zoom);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public string HoverInfo
		{
			get
			{
				return _hoverInfo;
			}
			[MemberNotNull("_hoverInfo")]
			set
			{
				if (!EqualityComparer<string>.Default.Equals(_hoverInfo, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HoverInfo);
					_hoverInfo = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HoverInfo);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand<WorldNpcRowViewModel> GoToNpcCommand => goToNpcCommand ?? (goToNpcCommand = new RelayCommand<WorldNpcRowViewModel>(GoToNpc));

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand ZoomInCommand => zoomInCommand ?? (zoomInCommand = new RelayCommand(ZoomIn));

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand ZoomOutCommand => zoomOutCommand ?? (zoomOutCommand = new RelayCommand(ZoomOut));

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand ZoomResetCommand => zoomResetCommand ?? (zoomResetCommand = new RelayCommand(ZoomReset));

		public event Action<int, int>? NavigateToTileRequested;

		[RelayCommand]
		private void GoToNpc(WorldNpcRowViewModel npc)
		{
			NavigateToTileRequested?.Invoke(npc.TileX, npc.TileY);
		}

		public ExplorationViewModel(CharacterFileService service)
		{
			_npcNames = service.NpcNames;
			_mapColors = service.MapColors;
			_tileNames = service.TileNames;
		}

		public void UpdateHover(int tileX, int tileY)
		{
			if (_world == null || tileX < 0 || tileY < 0 || tileX >= _world.Header.TilesWide || tileY >= _world.Header.TilesHigh)
			{
				HoverInfo = string.Empty;
				return;
			}
			WldTile wldTile = _world.Tiles[tileX, tileY];
			string value = (wldTile.IsActive ? _tileNames.TileVariantName(wldTile.Type, wldTile.U, wldTile.V) : ((wldTile.LiquidAmount > 0) ? LiquidName(wldTile.LiquidType) : "(vacio)"));
			string value2 = _tileNames.WallName(wldTile.Wall);
			HoverInfo = (string.IsNullOrEmpty(value2) ? $"({tileX}, {tileY}) - {value}" : $"({tileX}, {tileY}) - {value} / pared: {value2}");
		}

		private static string LiquidName(byte liquidType)
		{
			if (1 == 0)
			{
			}
			string result = liquidType switch
			{
				2 => "Lava", 
				3 => "Miel", 
				_ => "Agua", 
			};
			if (1 == 0)
			{
			}
			return result;
		}

		public void LoadFromPath(string wldPath)
		{
			try
			{
				StatusMessage = "Leyendo mundo...";
				WldWorld wldWorld = (_world = WldReader.Read(File.ReadAllBytes(wldPath)));
				StatusMessage = "Pintando mapa...";
				WorldImage = WorldRenderer.Render(wldWorld, _mapColors);
				_allNpcs = (from n in wldWorld.Npcs
					orderby _npcNames.GetName(n.Id)
					select new WorldNpcRowViewModel(n.Id, _npcNames.GetName(n.Id), n.TileX, n.TileY, n.Homeless)).ToList();
				NpcSearchText = string.Empty;
				Zoom = 1.0;
				HoverInfo = string.Empty;
				ApplyNpcFilter();
				HashSet<int> hashSet = wldWorld.Npcs.Select((WldNpc n) => n.Id).ToHashSet();
				MissingNpcs.Clear();
				foreach (int id in VanillaTownNpcRoster.Ids)
				{
					if (!hashSet.Contains(id))
					{
						MissingNpcs.Add(new MissingNpcRowViewModel(id, _npcNames.GetName(id)));
					}
				}
				WorldTitle = wldWorld.Header.Title;
				IsWorldLoaded = true;
				StatusMessage = $"'{wldWorld.Header.Title}' - {wldWorld.Header.TilesWide}x{wldWorld.Header.TilesHigh} tiles, {_allNpcs.Count} NPC(s) de pueblo, {MissingNpcs.Count} todavia sin conseguir.";
			}
			catch (Exception ex)
			{
				_world = null;
				IsWorldLoaded = false;
				StatusMessage = "Error al leer el mundo: " + ex.Message;
			}
		}

		[RelayCommand]
		private void ZoomIn()
		{
			Zoom *= 1.25;
		}

		[RelayCommand]
		private void ZoomOut()
		{
			Zoom /= 1.25;
		}

		[RelayCommand]
		private void ZoomReset()
		{
			Zoom = 1.0;
		}

		private void ApplyNpcFilter()
		{
			Npcs.Clear();
			IEnumerable<WorldNpcRowViewModel> enumerable;
			if (!string.IsNullOrWhiteSpace(NpcSearchText))
			{
				enumerable = _allNpcs.Where((WorldNpcRowViewModel n) => n.Name.Contains(NpcSearchText, StringComparison.OrdinalIgnoreCase));
			}
			else
			{
				IEnumerable<WorldNpcRowViewModel> allNpcs = _allNpcs;
				enumerable = allNpcs;
			}
			IEnumerable<WorldNpcRowViewModel> enumerable2 = enumerable;
			foreach (WorldNpcRowViewModel item in enumerable2)
			{
				Npcs.Add(item);
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnNpcSearchTextChanged(string value)
		{
			ApplyNpcFilter();
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnZoomChanged(double value)
		{
			double num = Math.Clamp(value, 0.1, 6.0);
			if (num != value)
			{
				Zoom = num;
			}
		}
	}
	public class FlagsViewModel : ObservableObject
	{
		private PlrCharacter? _character;

		private bool _suppressWriteback;

		[ObservableProperty]
		private bool _extraAccessory;

		[ObservableProperty]
		private bool _unlockedBiomeTorches;

		[ObservableProperty]
		private bool _usingBiomeTorches;

		[ObservableProperty]
		private bool _artisanBread;

		[ObservableProperty]
		private bool _vitalCrystal;

		[ObservableProperty]
		private bool _aegisFruit;

		[ObservableProperty]
		private bool _arcaneCrystal;

		[ObservableProperty]
		private bool _galaxyPearl;

		[ObservableProperty]
		private bool _gummyWorm;

		[ObservableProperty]
		private bool _ambrosia;

		[ObservableProperty]
		private bool _finishedDD2Event;

		[ObservableProperty]
		private bool _unlockedSuperMinecart;

		[ObservableProperty]
		private bool _usingSuperMinecart;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool ExtraAccessory
		{
			get
			{
				return _extraAccessory;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_extraAccessory, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ExtraAccessory);
					_extraAccessory = value;
					OnExtraAccessoryChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ExtraAccessory);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool UnlockedBiomeTorches
		{
			get
			{
				return _unlockedBiomeTorches;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_unlockedBiomeTorches, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.UnlockedBiomeTorches);
					_unlockedBiomeTorches = value;
					OnUnlockedBiomeTorchesChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.UnlockedBiomeTorches);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool UsingBiomeTorches
		{
			get
			{
				return _usingBiomeTorches;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_usingBiomeTorches, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.UsingBiomeTorches);
					_usingBiomeTorches = value;
					OnUsingBiomeTorchesChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.UsingBiomeTorches);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool ArtisanBread
		{
			get
			{
				return _artisanBread;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_artisanBread, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ArtisanBread);
					_artisanBread = value;
					OnArtisanBreadChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ArtisanBread);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool VitalCrystal
		{
			get
			{
				return _vitalCrystal;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_vitalCrystal, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.VitalCrystal);
					_vitalCrystal = value;
					OnVitalCrystalChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.VitalCrystal);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool AegisFruit
		{
			get
			{
				return _aegisFruit;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_aegisFruit, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AegisFruit);
					_aegisFruit = value;
					OnAegisFruitChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AegisFruit);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool ArcaneCrystal
		{
			get
			{
				return _arcaneCrystal;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_arcaneCrystal, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ArcaneCrystal);
					_arcaneCrystal = value;
					OnArcaneCrystalChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ArcaneCrystal);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool GalaxyPearl
		{
			get
			{
				return _galaxyPearl;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_galaxyPearl, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.GalaxyPearl);
					_galaxyPearl = value;
					OnGalaxyPearlChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.GalaxyPearl);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool GummyWorm
		{
			get
			{
				return _gummyWorm;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_gummyWorm, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.GummyWorm);
					_gummyWorm = value;
					OnGummyWormChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.GummyWorm);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool Ambrosia
		{
			get
			{
				return _ambrosia;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_ambrosia, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Ambrosia);
					_ambrosia = value;
					OnAmbrosiaChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Ambrosia);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool FinishedDD2Event
		{
			get
			{
				return _finishedDD2Event;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_finishedDD2Event, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.FinishedDD2Event);
					_finishedDD2Event = value;
					OnFinishedDD2EventChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.FinishedDD2Event);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool UnlockedSuperMinecart
		{
			get
			{
				return _unlockedSuperMinecart;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_unlockedSuperMinecart, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.UnlockedSuperMinecart);
					_unlockedSuperMinecart = value;
					OnUnlockedSuperMinecartChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.UnlockedSuperMinecart);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool UsingSuperMinecart
		{
			get
			{
				return _usingSuperMinecart;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_usingSuperMinecart, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.UsingSuperMinecart);
					_usingSuperMinecart = value;
					OnUsingSuperMinecartChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.UsingSuperMinecart);
				}
			}
		}

		public void LoadFrom(PlrCharacter character)
		{
			_character = null;
			_suppressWriteback = true;
			ExtraAccessory = character.ExtraAccessory;
			UnlockedBiomeTorches = character.UnlockedBiomeTorches;
			UsingBiomeTorches = character.UsingBiomeTorches;
			ArtisanBread = character.ExtraUsingFlags[0];
			VitalCrystal = character.ExtraUsingFlags[1];
			AegisFruit = character.ExtraUsingFlags[2];
			ArcaneCrystal = character.ExtraUsingFlags[3];
			GalaxyPearl = character.ExtraUsingFlags[4];
			GummyWorm = character.ExtraUsingFlags[5];
			Ambrosia = character.ExtraUsingFlags[6];
			FinishedDD2Event = character.FinishedDD2Event;
			UnlockedSuperMinecart = (character.SuperCartByte & 1) != 0;
			UsingSuperMinecart = (character.SuperCartByte & 1) != 0;
			_suppressWriteback = false;
			_character = character;
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnExtraAccessoryChanged(bool value)
		{
			if (!_suppressWriteback && _character != null)
			{
				_character.ExtraAccessory = value;
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnUnlockedBiomeTorchesChanged(bool value)
		{
			if (!_suppressWriteback && _character != null)
			{
				_character.UnlockedBiomeTorches = value;
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnUsingBiomeTorchesChanged(bool value)
		{
			if (!_suppressWriteback && _character != null)
			{
				_character.UsingBiomeTorches = value;
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnArtisanBreadChanged(bool value)
		{
			if (!_suppressWriteback && _character != null)
			{
				_character.ExtraUsingFlags[0] = value;
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnVitalCrystalChanged(bool value)
		{
			if (!_suppressWriteback && _character != null)
			{
				_character.ExtraUsingFlags[1] = value;
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnAegisFruitChanged(bool value)
		{
			if (!_suppressWriteback && _character != null)
			{
				_character.ExtraUsingFlags[2] = value;
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnArcaneCrystalChanged(bool value)
		{
			if (!_suppressWriteback && _character != null)
			{
				_character.ExtraUsingFlags[3] = value;
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnGalaxyPearlChanged(bool value)
		{
			if (!_suppressWriteback && _character != null)
			{
				_character.ExtraUsingFlags[4] = value;
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnGummyWormChanged(bool value)
		{
			if (!_suppressWriteback && _character != null)
			{
				_character.ExtraUsingFlags[5] = value;
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnAmbrosiaChanged(bool value)
		{
			if (!_suppressWriteback && _character != null)
			{
				_character.ExtraUsingFlags[6] = value;
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnFinishedDD2EventChanged(bool value)
		{
			if (!_suppressWriteback && _character != null)
			{
				_character.FinishedDD2Event = value;
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnUnlockedSuperMinecartChanged(bool value)
		{
			if (!_suppressWriteback && _character != null)
			{
				_character.SuperCartByte = (byte)((_character.SuperCartByte & -2) | (value ? 1 : 0));
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnUsingSuperMinecartChanged(bool value)
		{
			if (!_suppressWriteback && _character != null)
			{
				_character.SuperCartByte = (byte)((_character.SuperCartByte & -2) | (value ? 1 : 0));
			}
		}
	}
	public sealed class HairDyeOptionViewModel(int index, string displayName, string? iconPath)
	{
		public int Index { get; } = index;

		public string DisplayName { get; } = displayName;

		public string? IconPath { get; } = iconPath;
	}
	public sealed class HairOptionViewModel(int id, WriteableBitmap thumbnail)
	{
		public int Id { get; } = id;

		public WriteableBitmap Thumbnail { get; } = thumbnail;
	}
	public class ItemEditViewModel : ObservableObject
	{
		private readonly CharacterFileService _service;

		private PrefixCategory _currentCategories = PrefixCategory.None;

		[ObservableProperty]
		private ItemSlotViewModel? _slot;

		[ObservableProperty]
		private PrefixMetaButtonViewModel? _selectedMeta;

		[ObservableProperty]
		private PrefixGroupButtonViewModel? _selectedGroup;

		[ObservableProperty]
		private bool _canHavePrefix;

		[ObservableProperty]
		private string _noPrefixMessage = "Selecciona un slot para editarlo.";

		[ObservableProperty]
		private string _categoriesLabel = string.Empty;

		[ObservableProperty]
		private bool _hasSelection;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand<PrefixMetaButtonViewModel?>? selectMetaCommand;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand<PrefixGroupButtonViewModel?>? selectGroupCommand;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand<PrefixCatalogEntryViewModel?>? applyPrefixCommand;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand? clearPrefixCommand;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand? applyBestPrefixCommand;

		public ObservableCollection<PrefixMetaButtonViewModel> Metas { get; }

		public ObservableCollection<PrefixGroupButtonViewModel> Groups { get; } = new ObservableCollection<PrefixGroupButtonViewModel>();

		public ObservableCollection<PrefixCatalogEntryViewModel> Prefixes { get; } = new ObservableCollection<PrefixCatalogEntryViewModel>();

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public ItemSlotViewModel? Slot
		{
			get
			{
				return _slot;
			}
			set
			{
				if (!EqualityComparer<ItemSlotViewModel>.Default.Equals(_slot, value))
				{
					ItemSlotViewModel slot = _slot;
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Slot);
					_slot = value;
					OnSlotChanged(slot, value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Slot);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public PrefixMetaButtonViewModel? SelectedMeta
		{
			get
			{
				return _selectedMeta;
			}
			set
			{
				if (!EqualityComparer<PrefixMetaButtonViewModel>.Default.Equals(_selectedMeta, value))
				{
					PrefixMetaButtonViewModel selectedMeta = _selectedMeta;
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedMeta);
					_selectedMeta = value;
					OnSelectedMetaChanged(selectedMeta, value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedMeta);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public PrefixGroupButtonViewModel? SelectedGroup
		{
			get
			{
				return _selectedGroup;
			}
			set
			{
				if (!EqualityComparer<PrefixGroupButtonViewModel>.Default.Equals(_selectedGroup, value))
				{
					PrefixGroupButtonViewModel selectedGroup = _selectedGroup;
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedGroup);
					_selectedGroup = value;
					OnSelectedGroupChanged(selectedGroup, value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedGroup);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool CanHavePrefix
		{
			get
			{
				return _canHavePrefix;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_canHavePrefix, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CanHavePrefix);
					_canHavePrefix = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CanHavePrefix);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public string NoPrefixMessage
		{
			get
			{
				return _noPrefixMessage;
			}
			[MemberNotNull("_noPrefixMessage")]
			set
			{
				if (!EqualityComparer<string>.Default.Equals(_noPrefixMessage, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.NoPrefixMessage);
					_noPrefixMessage = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.NoPrefixMessage);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public string CategoriesLabel
		{
			get
			{
				return _categoriesLabel;
			}
			[MemberNotNull("_categoriesLabel")]
			set
			{
				if (!EqualityComparer<string>.Default.Equals(_categoriesLabel, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CategoriesLabel);
					_categoriesLabel = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CategoriesLabel);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool HasSelection
		{
			get
			{
				return _hasSelection;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_hasSelection, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HasSelection);
					_hasSelection = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HasSelection);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand<PrefixMetaButtonViewModel?> SelectMetaCommand => selectMetaCommand ?? (selectMetaCommand = new RelayCommand<PrefixMetaButtonViewModel>(SelectMeta));

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand<PrefixGroupButtonViewModel?> SelectGroupCommand => selectGroupCommand ?? (selectGroupCommand = new RelayCommand<PrefixGroupButtonViewModel>(SelectGroup));

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand<PrefixCatalogEntryViewModel?> ApplyPrefixCommand => applyPrefixCommand ?? (applyPrefixCommand = new RelayCommand<PrefixCatalogEntryViewModel>(ApplyPrefix));

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand ClearPrefixCommand => clearPrefixCommand ?? (clearPrefixCommand = new RelayCommand(ClearPrefix));

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand ApplyBestPrefixCommand => applyBestPrefixCommand ?? (applyBestPrefixCommand = new RelayCommand(ApplyBestPrefix));

		public ItemEditViewModel(CharacterFileService service)
		{
			_service = service;
			Metas = new ObservableCollection<PrefixMetaButtonViewModel>(PrefixGroupCatalog.Metas.Select((PrefixMeta m) => new PrefixMetaButtonViewModel(m)));
			Refresh();
		}

		private void OnSlotPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			Refresh();
		}

		private void Refresh()
		{
			ItemSlotViewModel slot = Slot;
			HasSelection = slot != null && !slot.IsEmpty;
			if (slot == null || slot.IsEmpty)
			{
				CanHavePrefix = false;
				NoPrefixMessage = ((slot == null) ? "Selecciona un slot para editarlo." : "Este slot está vacío.");
				CategoriesLabel = string.Empty;
				_currentCategories = PrefixCategory.None;
				Groups.Clear();
				Prefixes.Clear();
				return;
			}
			_currentCategories = PrefixEligibility.For(slot.Item, _service.PrefixRules, _service.CalamityCatalog);
			CanHavePrefix = _currentCategories != PrefixCategory.None;
			NoPrefixMessage = (CanHavePrefix ? string.Empty : "Este objeto no admite ningún prefijo.");
			CategoriesLabel = DescribeCategories(_currentCategories);
			if (!CanHavePrefix)
			{
				Groups.Clear();
				Prefixes.Clear();
				return;
			}
			PrefixMetaButtonViewModel prefixMetaButtonViewModel = ((SelectedMeta != null && HasApplicableGroups(SelectedMeta)) ? SelectedMeta : Metas.FirstOrDefault(HasApplicableGroups));
			if (prefixMetaButtonViewModel == SelectedMeta)
			{
				RebuildGroups();
			}
			else
			{
				SelectedMeta = prefixMetaButtonViewModel;
			}
		}

		private bool HasApplicableGroups(PrefixMetaButtonViewModel meta)
		{
			ItemSlotViewModel slot = Slot;
			return PrefixGroupCatalog.GroupsFor(meta.Meta, _currentCategories, slot.IsCalamity, slot.Item.Id, _service.PrefixRules).Any();
		}

		[RelayCommand]
		private void SelectMeta(PrefixMetaButtonViewModel? meta)
		{
			if (meta != null && meta != SelectedMeta)
			{
				SelectedMeta = meta;
			}
		}

		[RelayCommand]
		private void SelectGroup(PrefixGroupButtonViewModel? group)
		{
			if (group != null && group != SelectedGroup)
			{
				SelectedGroup = group;
			}
		}

		private void RebuildGroups()
		{
			Groups.Clear();
			ItemSlotViewModel slot = Slot;
			if (slot == null || SelectedMeta == null)
			{
				Prefixes.Clear();
				return;
			}
			foreach (PrefixGroup item in PrefixGroupCatalog.GroupsFor(SelectedMeta.Meta, _currentCategories, slot.IsCalamity, slot.Item.Id, _service.PrefixRules))
			{
				Groups.Add(new PrefixGroupButtonViewModel(item));
			}
			PrefixGroupButtonViewModel prefixGroupButtonViewModel = Groups.FirstOrDefault();
			if (prefixGroupButtonViewModel == SelectedGroup)
			{
				RebuildPrefixes();
			}
			else
			{
				SelectedGroup = prefixGroupButtonViewModel;
			}
		}

		private void RebuildPrefixes()
		{
			Prefixes.Clear();
			ItemSlotViewModel slot = Slot;
			PrefixGroupButtonViewModel selectedGroup = SelectedGroup;
			if (slot == null || selectedGroup == null)
			{
				return;
			}
			foreach (int item in PrefixGroupCatalog.PrefixIdsFor(selectedGroup.Group, slot.IsCalamity, slot.Item.Id, _service.PrefixRules))
			{
				VanillaPrefixEntryData vanillaPrefixEntryData = _service.VanillaPrefixCatalog.ById(item);
				string displayName = vanillaPrefixEntryData?.Es ?? vanillaPrefixEntryData?.En ?? $"Prefijo #{item}";
				bool isCalamity = item >= 85;
				bool isCurrent = !slot.Item.Prefix.IsCalamity && slot.Item.Prefix.VanillaId == item;
				Prefixes.Add(new PrefixCatalogEntryViewModel(displayName, ItemPrefix.Vanilla((byte)item), isCalamity, isCurrent));
			}
		}

		[RelayCommand]
		private void ApplyPrefix(PrefixCatalogEntryViewModel? entry)
		{
			if (Slot != null && entry != null)
			{
				Slot.SetPrefix(entry.Prefix);
				RebuildPrefixes();
			}
		}

		[RelayCommand]
		private void ClearPrefix()
		{
			if (Slot != null)
			{
				Slot.SetPrefix(ItemPrefix.None);
				RebuildPrefixes();
			}
		}

		[RelayCommand]
		private void ApplyBestPrefix()
		{
			if (Slot != null && Slot.ApplyBestPrefixCommand.CanExecute(null))
			{
				Slot.ApplyBestPrefixCommand.Execute(null);
				RebuildPrefixes();
			}
		}

		private static string DescribeCategories(PrefixCategory cats)
		{
			List<string> list = new List<string>();
			if (cats.HasFlag(PrefixCategory.Melee))
			{
				list.Add("Cuerpo a cuerpo");
			}
			if (cats.HasFlag(PrefixCategory.Ranged))
			{
				list.Add("A distancia");
			}
			if (cats.HasFlag(PrefixCategory.Magic))
			{
				list.Add("Magia");
			}
			if (cats.HasFlag(PrefixCategory.Summon))
			{
				list.Add("Invocación");
			}
			if (cats.HasFlag(PrefixCategory.Accessory))
			{
				list.Add("Accesorio");
			}
			return string.Join(" · ", list);
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnSlotChanged(ItemSlotViewModel? oldValue, ItemSlotViewModel? newValue)
		{
			if (oldValue != null)
			{
				oldValue.PropertyChanged -= OnSlotPropertyChanged;
			}
			if (newValue != null)
			{
				newValue.PropertyChanged += OnSlotPropertyChanged;
			}
			Refresh();
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnSelectedMetaChanged(PrefixMetaButtonViewModel? oldValue, PrefixMetaButtonViewModel? newValue)
		{
			if (oldValue != null)
			{
				oldValue.IsSelected = false;
			}
			if (newValue != null)
			{
				newValue.IsSelected = true;
			}
			RebuildGroups();
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnSelectedGroupChanged(PrefixGroupButtonViewModel? oldValue, PrefixGroupButtonViewModel? newValue)
		{
			if (oldValue != null)
			{
				oldValue.IsSelected = false;
			}
			if (newValue != null)
			{
				newValue.IsSelected = true;
			}
			RebuildPrefixes();
		}
	}
	public class ItemSlotViewModel : ObservableObject
	{
		private readonly CharacterFileService _service;

		private readonly Action<ItemSlotViewModel>? _requestPick;

		private bool _suppressCountWriteback;

		private bool _suppressIdWriteback;

		private bool _suppressPrefixIdWriteback;

		[ObservableProperty]
		private string _displayName = string.Empty;

		[ObservableProperty]
		private int _count;

		[ObservableProperty]
		private bool _isCalamity;

		[ObservableProperty]
		private bool _isEmpty = true;

		[ObservableProperty]
		private string _prefixDisplay = string.Empty;

		[ObservableProperty]
		private bool _hasBestPrefixSuggestion;

		[ObservableProperty]
		private string? _iconPath;

		[ObservableProperty]
		private string? _statsTooltip;

		[ObservableProperty]
		private bool _isSelected;

		[ObservableProperty]
		private int _itemId;

		[ObservableProperty]
		private int _prefixId;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand? clearCommand;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand? chooseFromLibraryCommand;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand? applyBestPrefixCommand;

		public int SlotIndex { get; }

		public string ContainerName { get; }

		public GameItem Item { get; private set; } = GameItem.Empty;

		public bool IsNotEmpty => !IsEmpty;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public string DisplayName
		{
			get
			{
				return _displayName;
			}
			[MemberNotNull("_displayName")]
			set
			{
				if (!EqualityComparer<string>.Default.Equals(_displayName, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.DisplayName);
					_displayName = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.DisplayName);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public int Count
		{
			get
			{
				return _count;
			}
			set
			{
				if (!EqualityComparer<int>.Default.Equals(_count, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Count);
					_count = value;
					OnCountChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Count);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool IsCalamity
		{
			get
			{
				return _isCalamity;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_isCalamity, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsCalamity);
					_isCalamity = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsCalamity);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool IsEmpty
		{
			get
			{
				return _isEmpty;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_isEmpty, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsEmpty);
					_isEmpty = value;
					OnIsEmptyChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsEmpty);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public string PrefixDisplay
		{
			get
			{
				return _prefixDisplay;
			}
			[MemberNotNull("_prefixDisplay")]
			set
			{
				if (!EqualityComparer<string>.Default.Equals(_prefixDisplay, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PrefixDisplay);
					_prefixDisplay = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PrefixDisplay);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool HasBestPrefixSuggestion
		{
			get
			{
				return _hasBestPrefixSuggestion;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_hasBestPrefixSuggestion, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HasBestPrefixSuggestion);
					_hasBestPrefixSuggestion = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HasBestPrefixSuggestion);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public string? IconPath
		{
			get
			{
				return _iconPath;
			}
			set
			{
				if (!EqualityComparer<string>.Default.Equals(_iconPath, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IconPath);
					_iconPath = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IconPath);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public string? StatsTooltip
		{
			get
			{
				return _statsTooltip;
			}
			set
			{
				if (!EqualityComparer<string>.Default.Equals(_statsTooltip, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.StatsTooltip);
					_statsTooltip = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.StatsTooltip);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool IsSelected
		{
			get
			{
				return _isSelected;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_isSelected, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsSelected);
					_isSelected = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsSelected);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public int ItemId
		{
			get
			{
				return _itemId;
			}
			set
			{
				if (!EqualityComparer<int>.Default.Equals(_itemId, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ItemId);
					_itemId = value;
					OnItemIdChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ItemId);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public int PrefixId
		{
			get
			{
				return _prefixId;
			}
			set
			{
				if (!EqualityComparer<int>.Default.Equals(_prefixId, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PrefixId);
					_prefixId = value;
					OnPrefixIdChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PrefixId);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand ClearCommand => clearCommand ?? (clearCommand = new RelayCommand(Clear));

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand ChooseFromLibraryCommand => chooseFromLibraryCommand ?? (chooseFromLibraryCommand = new RelayCommand(ChooseFromLibrary));

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand ApplyBestPrefixCommand => applyBestPrefixCommand ?? (applyBestPrefixCommand = new RelayCommand(ApplyBestPrefix));

		public ItemSlotViewModel(CharacterFileService service, int slotIndex, string containerName, GameItem item, Action<ItemSlotViewModel>? requestPick = null)
		{
			_service = service;
			SlotIndex = slotIndex;
			ContainerName = containerName;
			_requestPick = requestPick;
			UpdateFrom(item);
		}

		public void UpdateFrom(GameItem item)
		{
			Item = item;
			IsEmpty = item.IsEmpty;
			IsCalamity = item.IsCalamity;
			_suppressCountWriteback = true;
			Count = item.Count;
			_suppressCountWriteback = false;
			_suppressIdWriteback = true;
			ItemId = ((!item.IsEmpty) ? item.Id : 0);
			_suppressIdWriteback = false;
			_suppressPrefixIdWriteback = true;
			PrefixId = ((!item.Prefix.IsCalamity) ? item.Prefix.VanillaId : 0);
			_suppressPrefixIdWriteback = false;
			if (item.IsEmpty)
			{
				DisplayName = string.Empty;
				PrefixDisplay = string.Empty;
				HasBestPrefixSuggestion = false;
				IconPath = null;
				StatsTooltip = null;
				return;
			}
			if (item.IsCalamity)
			{
				CalamityCatalogEntry calamityCatalogEntry = _service.CalamityCatalog.BySyntheticId(item.Id);
				DisplayName = calamityCatalogEntry?.DisplayName ?? $"Calamity #{item.Id}";
				IconPath = ((calamityCatalogEntry != null && calamityCatalogEntry.Icon != null) ? ("pack://siteoforigin:,,,/Assets/calamity/icons/" + calamityCatalogEntry.Icon) : null);
			}
			else
			{
				DisplayName = _service.VanillaCatalog.GetName(item.Id);
				IconPath = VanillaIconResolver.GetIconPath(item.Id);
			}
			StatsTooltip = ItemStatsFormatter.Format(item.IsCalamity, item.Id, _service.VanillaStats, _service.CalamityCatalog, _service.VanillaCategories);
			RefreshPrefixDisplay();
			ItemPrefix? itemPrefix = PrefixSuggester.Suggest(item, _service.CalamityCatalog, _service.BestPrefixes, _service.RoguePrefixCatalog);
			HasBestPrefixSuggestion = itemPrefix.HasValue && !itemPrefix.Value.Equals(item.Prefix);
		}

		public void PlaceItem(int id)
		{
			GameItem gameItem = new GameItem
			{
				Id = id,
				Count = 1
			};
			ItemPrefix? itemPrefix = PrefixSuggester.Suggest(gameItem, _service.CalamityCatalog, _service.BestPrefixes, _service.RoguePrefixCatalog);
			if (itemPrefix.HasValue)
			{
				gameItem.Prefix = itemPrefix.Value;
			}
			UpdateFrom(gameItem);
		}

		public void SwapWith(ItemSlotViewModel other)
		{
			GameItem item = Item;
			UpdateFrom(other.Item);
			other.UpdateFrom(item);
		}

		[RelayCommand]
		private void Clear()
		{
			UpdateFrom(GameItem.Empty);
		}

		[RelayCommand]
		private void ChooseFromLibrary()
		{
			_requestPick?.Invoke(this);
		}

		private void RefreshPrefixDisplay()
		{
			_suppressPrefixIdWriteback = true;
			PrefixId = ((!Item.Prefix.IsCalamity) ? Item.Prefix.VanillaId : 0);
			_suppressPrefixIdWriteback = false;
			ItemPrefix prefix = Item.Prefix;
			if (prefix.IsCalamity)
			{
				RoguePrefixEntryData roguePrefixEntryData = _service.RoguePrefixCatalog.ById(prefix.SyntheticId);
				PrefixDisplay = roguePrefixEntryData?.Es ?? roguePrefixEntryData?.En ?? string.Empty;
			}
			else if (!prefix.IsNone)
			{
				VanillaPrefixEntryData vanillaPrefixEntryData = _service.VanillaPrefixCatalog.ById(prefix.VanillaId);
				PrefixDisplay = vanillaPrefixEntryData?.Es ?? vanillaPrefixEntryData?.En ?? $"Prefijo #{prefix.VanillaId}";
			}
			else
			{
				PrefixDisplay = string.Empty;
			}
		}

		[RelayCommand]
		private void ApplyBestPrefix()
		{
			ItemPrefix? itemPrefix = PrefixSuggester.Suggest(Item, _service.CalamityCatalog, _service.BestPrefixes, _service.RoguePrefixCatalog);
			if (itemPrefix.HasValue)
			{
				SetPrefix(itemPrefix.Value);
			}
		}

		public void SetPrefix(ItemPrefix prefix)
		{
			Item.Prefix = prefix;
			RefreshPrefixDisplay();
			ItemPrefix? itemPrefix = PrefixSuggester.Suggest(Item, _service.CalamityCatalog, _service.BestPrefixes, _service.RoguePrefixCatalog);
			HasBestPrefixSuggestion = itemPrefix.HasValue && !itemPrefix.Value.Equals(Item.Prefix);
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnCountChanged(int value)
		{
			if (!_suppressCountWriteback && !Item.IsEmpty)
			{
				int num = Math.Clamp(value, 1, 9999);
				Item.Count = num;
				if (num != value)
				{
					_suppressCountWriteback = true;
					Count = num;
					_suppressCountWriteback = false;
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnIsEmptyChanged(bool value)
		{
			OnPropertyChanged("IsNotEmpty");
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnItemIdChanged(int value)
		{
			if (!_suppressIdWriteback && value > 0 && value != Item.Id)
			{
				PlaceItem(value);
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnPrefixIdChanged(int value)
		{
			if (!_suppressPrefixIdWriteback && !Item.IsEmpty)
			{
				SetPrefix(ItemPrefix.Vanilla((byte)Math.Clamp(value, 0, 255)));
			}
		}
	}
	public sealed class LibraryItemViewModel(string displayName, bool isCalamity, string? iconPath, int id, string category, string? statsTooltip = null)
	{
		public string DisplayName { get; } = displayName;

		public bool IsCalamity { get; } = isCalamity;

		public string? IconPath { get; } = iconPath;

		public int Id { get; } = id;

		public string Category { get; } = category;

		public string? StatsTooltip { get; } = statsTooltip;
	}
	public class LibraryViewModel : ObservableObject
	{
		private const int MaxResults = 300;

		private readonly List<LibraryItemViewModel> _all;

		[ObservableProperty]
		private string _searchText = string.Empty;

		[ObservableProperty]
		private string _resultsSummary = string.Empty;

		[ObservableProperty]
		private ItemSlotViewModel? _pickTarget;

		[ObservableProperty]
		private CategoryNodeViewModel? _selectedCategory;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand<CategoryNodeViewModel>? selectCategoryCommand;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand? clearCategoryCommand;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand<LibraryItemViewModel>? placeInTargetCommand;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand? cancelPickCommand;

		public bool IsPicking => PickTarget != null;

		public ObservableCollection<LibraryItemViewModel> Results { get; } = new ObservableCollection<LibraryItemViewModel>();

		public ObservableCollection<CategoryNodeViewModel> RootCategories { get; } = new ObservableCollection<CategoryNodeViewModel>();

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public string SearchText
		{
			get
			{
				return _searchText;
			}
			[MemberNotNull("_searchText")]
			set
			{
				if (!EqualityComparer<string>.Default.Equals(_searchText, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SearchText);
					_searchText = value;
					OnSearchTextChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SearchText);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public string ResultsSummary
		{
			get
			{
				return _resultsSummary;
			}
			[MemberNotNull("_resultsSummary")]
			set
			{
				if (!EqualityComparer<string>.Default.Equals(_resultsSummary, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ResultsSummary);
					_resultsSummary = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ResultsSummary);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public ItemSlotViewModel? PickTarget
		{
			get
			{
				return _pickTarget;
			}
			set
			{
				if (!EqualityComparer<ItemSlotViewModel>.Default.Equals(_pickTarget, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PickTarget);
					_pickTarget = value;
					OnPickTargetChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PickTarget);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public CategoryNodeViewModel? SelectedCategory
		{
			get
			{
				return _selectedCategory;
			}
			set
			{
				if (!EqualityComparer<CategoryNodeViewModel>.Default.Equals(_selectedCategory, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedCategory);
					_selectedCategory = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedCategory);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand<CategoryNodeViewModel> SelectCategoryCommand => selectCategoryCommand ?? (selectCategoryCommand = new RelayCommand<CategoryNodeViewModel>(SelectCategory));

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand ClearCategoryCommand => clearCategoryCommand ?? (clearCategoryCommand = new RelayCommand(ClearCategory));

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand<LibraryItemViewModel> PlaceInTargetCommand => placeInTargetCommand ?? (placeInTargetCommand = new RelayCommand<LibraryItemViewModel>(PlaceInTarget));

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand CancelPickCommand => cancelPickCommand ?? (cancelPickCommand = new RelayCommand(CancelPick));

		public event Action? ItemPlaced;

		public LibraryViewModel(CharacterFileService service)
		{
			_all = new List<LibraryItemViewModel>();
			foreach (var item3 in service.VanillaCatalog.AllEntries())
			{
				int item = item3.Id;
				string item2 = item3.Name;
				string statsTooltip = ItemStatsFormatter.Format(isCalamity: false, item, service.VanillaStats, service.CalamityCatalog, service.VanillaCategories);
				_all.Add(new LibraryItemViewModel(item2, isCalamity: false, VanillaIconResolver.GetIconPath(item), item, service.VanillaCategories.GetCategory(item), statsTooltip));
			}
			foreach (CalamityCatalogEntry entry in service.CalamityCatalog.Entries)
			{
				string iconPath = ((entry.Icon != null) ? ("pack://siteoforigin:,,,/Assets/calamity/icons/" + entry.Icon) : null);
				string statsTooltip2 = ItemStatsFormatter.Format(isCalamity: true, entry.SyntheticId, service.VanillaStats, service.CalamityCatalog, service.VanillaCategories);
				_all.Add(new LibraryItemViewModel(entry.DisplayName, isCalamity: true, iconPath, entry.SyntheticId, entry.Category, statsTooltip2));
			}
			BuildCategoryTree();
			ApplyFilter();
		}

		private void BuildCategoryTree()
		{
			Dictionary<string, CategoryNodeViewModel> byPath = new Dictionary<string, CategoryNodeViewModel>();
			foreach (LibraryItemViewModel item in _all)
			{
				string parentPath = string.Empty;
				string[] array = item.Category.Split('/');
				foreach (string name in array)
				{
					CategoryNodeViewModel categoryNodeViewModel = GetOrCreate(parentPath, name);
					categoryNodeViewModel.ItemCount++;
					CategoryNodeViewModel categoryNodeViewModel2 = categoryNodeViewModel;
					if (categoryNodeViewModel2.IconPath == null)
					{
						string text = (categoryNodeViewModel2.IconPath = item.IconPath);
					}
					parentPath = categoryNodeViewModel.FullPath;
				}
			}
			CategoryNodeViewModel GetOrCreate(string text3, string text4)
			{
				string text2 = ((text3.Length == 0) ? text4 : (text3 + "/" + text4));
				if (byPath.TryGetValue(text2, out CategoryNodeViewModel value))
				{
					return value;
				}
				CategoryNodeViewModel categoryNodeViewModel3 = new CategoryNodeViewModel(text4, text2);
				byPath[text2] = categoryNodeViewModel3;
				if (text3.Length == 0)
				{
					RootCategories.Add(categoryNodeViewModel3);
				}
				else
				{
					byPath[text3].Children.Add(categoryNodeViewModel3);
				}
				return categoryNodeViewModel3;
			}
		}

		[RelayCommand]
		private void SelectCategory(CategoryNodeViewModel node)
		{
			if (SelectedCategory != null)
			{
				SelectedCategory.IsSelected = false;
			}
			if (SelectedCategory == node)
			{
				SelectedCategory = null;
			}
			else
			{
				SelectedCategory = node;
				node.IsSelected = true;
			}
			ApplyFilter();
		}

		[RelayCommand]
		private void ClearCategory()
		{
			if (SelectedCategory != null)
			{
				SelectedCategory.IsSelected = false;
			}
			SelectedCategory = null;
			ApplyFilter();
		}

		private void ApplyFilter()
		{
			Results.Clear();
			IEnumerable<LibraryItemViewModel> source = _all;
			string categoryPrefix = SelectedCategory?.FullPath;
			if (categoryPrefix != null)
			{
				source = source.Where((LibraryItemViewModel i) => i.Category == categoryPrefix || i.Category.StartsWith(categoryPrefix + "/", StringComparison.Ordinal));
			}
			bool flag = !string.IsNullOrWhiteSpace(SearchText);
			if (flag)
			{
				source = source.Where((LibraryItemViewModel i) => i.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
			}
			if (!flag && categoryPrefix == null)
			{
				ResultsSummary = $"{_all.Count} objetos en total (vanilla + Calamity) - escribe para buscar o elige una carpeta.";
				return;
			}
			List<LibraryItemViewModel> list = source.ToList();
			foreach (LibraryItemViewModel item in list.Take(300))
			{
				Results.Add(item);
			}
			string value = ((categoryPrefix != null) ? (" en \"" + SelectedCategory.Name + "\"") : string.Empty);
			ResultsSummary = ((list.Count > 300) ? $"Mostrando {300} de {list.Count} resultados{value} - afina la busqueda." : $"{list.Count} resultado(s){value}.");
		}

		[RelayCommand]
		private void PlaceInTarget(LibraryItemViewModel entry)
		{
			if (PickTarget != null)
			{
				PickTarget.PlaceItem(entry.Id);
				PickTarget = null;
				ItemPlaced?.Invoke();
			}
		}

		[RelayCommand]
		private void CancelPick()
		{
			PickTarget = null;
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnSearchTextChanged(string value)
		{
			ApplyFilter();
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnPickTargetChanged(ItemSlotViewModel? value)
		{
			OnPropertyChanged("IsPicking");
		}
	}
	public class MainViewModel : ObservableObject
	{
		private readonly CharacterFileService _service;

		private LoadedCharacter? _loaded;

		private readonly DispatcherTimer _saveConfirmationTimer;

		private const int InicioTabIndex = 0;

		private const int PersonajeTabIndex = 1;

		private const int BuildsTabIndex = 2;

		private const int NovedadesTabIndex = 3;

		private const int ExploracionTabIndex = 4;

		private const int AcercaDeTabIndex = 5;

		private const int ObjetosInnerTabIndex = 0;

		[ObservableProperty]
		private string _statusMessage;

		[ObservableProperty]
		private string? _characterName;

		[ObservableProperty]
		private bool _isCharacterLoaded;

		[ObservableProperty]
		private bool _hasCalamityData;

		[ObservableProperty]
		private int _selectedTabIndex;

		[ObservableProperty]
		private int _personajeInnerTabIndex;

		[ObservableProperty]
		private bool _saveConfirmationVisible;

		[ObservableProperty]
		private EquipmentGroupViewModel? _equipmentGroup;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand<string>? goToTabCommand;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand? saveCommand;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand? researchAllCommand;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand<BuildClassGear?>? autoEquipCommand;

		public ObservableCollection<ContainerViewModel> Containers { get; }

		public ObservableCollection<ResearchRowViewModel> Research { get; }

		public BuildsViewModel Builds { get; }

		public WhatsNewViewModel WhatsNew { get; }

		public ChangelogViewModel Changelog { get; }

		public AboutViewModel About { get; }

		public ExplorationViewModel Exploration { get; }

		public LibraryViewModel Library { get; }

		public AppearanceViewModel Appearance { get; }

		public ServersViewModel Servers { get; }

		public FlagsViewModel Flags { get; }

		public VersionEditorViewModel VersionEditor { get; }

		public BuffsViewModel Buffs { get; }

		public ItemEditViewModel ItemEdit { get; }

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public string StatusMessage
		{
			get
			{
				return _statusMessage;
			}
			[MemberNotNull("_statusMessage")]
			set
			{
				if (!EqualityComparer<string>.Default.Equals(_statusMessage, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.StatusMessage);
					_statusMessage = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.StatusMessage);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public string? CharacterName
		{
			get
			{
				return _characterName;
			}
			set
			{
				if (!EqualityComparer<string>.Default.Equals(_characterName, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CharacterName);
					_characterName = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CharacterName);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool IsCharacterLoaded
		{
			get
			{
				return _isCharacterLoaded;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_isCharacterLoaded, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsCharacterLoaded);
					_isCharacterLoaded = value;
					OnIsCharacterLoadedChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsCharacterLoaded);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool HasCalamityData
		{
			get
			{
				return _hasCalamityData;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_hasCalamityData, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HasCalamityData);
					_hasCalamityData = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HasCalamityData);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public int SelectedTabIndex
		{
			get
			{
				return _selectedTabIndex;
			}
			set
			{
				if (!EqualityComparer<int>.Default.Equals(_selectedTabIndex, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedTabIndex);
					_selectedTabIndex = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedTabIndex);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public int PersonajeInnerTabIndex
		{
			get
			{
				return _personajeInnerTabIndex;
			}
			set
			{
				if (!EqualityComparer<int>.Default.Equals(_personajeInnerTabIndex, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PersonajeInnerTabIndex);
					_personajeInnerTabIndex = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PersonajeInnerTabIndex);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool SaveConfirmationVisible
		{
			get
			{
				return _saveConfirmationVisible;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_saveConfirmationVisible, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SaveConfirmationVisible);
					_saveConfirmationVisible = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SaveConfirmationVisible);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public EquipmentGroupViewModel? EquipmentGroup
		{
			get
			{
				return _equipmentGroup;
			}
			set
			{
				if (!EqualityComparer<EquipmentGroupViewModel>.Default.Equals(_equipmentGroup, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.EquipmentGroup);
					_equipmentGroup = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.EquipmentGroup);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand<string> GoToTabCommand => goToTabCommand ?? (goToTabCommand = new RelayCommand<string>(GoToTab));

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand SaveCommand => saveCommand ?? (saveCommand = new RelayCommand(Save, () => IsCharacterLoaded));

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand ResearchAllCommand => researchAllCommand ?? (researchAllCommand = new RelayCommand(ResearchAll, () => IsCharacterLoaded));

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand<BuildClassGear?> AutoEquipCommand => autoEquipCommand ?? (autoEquipCommand = new RelayCommand<BuildClassGear>(AutoEquip, (BuildClassGear? _) => IsCharacterLoaded));

		public MainViewModel()
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Expected O, but got Unknown
			_service = new CharacterFileService();
			_saveConfirmationTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromSeconds(1.5)
			};
			_statusMessage = "Sin personaje cargado.";
			Containers = new ObservableCollection<ContainerViewModel>();
			Research = new ObservableCollection<ResearchRowViewModel>();
			About = new AboutViewModel();
			Servers = new ServersViewModel();
			Flags = new FlagsViewModel();
			VersionEditor = new VersionEditorViewModel();
			base..ctor();
			Builds = new BuildsViewModel(_service.VanillaBuilds, _service.CalamityBuilds, _service);
			WhatsNew = new WhatsNewViewModel(_service.WhatsNew);
			Changelog = new ChangelogViewModel(_service.Changelog);
			Exploration = new ExplorationViewModel(_service);
			Library = new LibraryViewModel(_service);
			Appearance = new AppearanceViewModel(_service);
			Library.ItemPlaced += delegate
			{
				SelectedTabIndex = 1;
				PersonajeInnerTabIndex = 0;
			};
			Buffs = new BuffsViewModel(_service);
			ItemEdit = new ItemEditViewModel(_service);
			_saveConfirmationTimer.Tick += delegate
			{
				SaveConfirmationVisible = false;
				_saveConfirmationTimer.Stop();
			};
		}

		public void SelectSlot(ItemSlotViewModel slot)
		{
			if (ItemEdit.Slot != null)
			{
				ItemEdit.Slot.IsSelected = false;
			}
			slot.IsSelected = true;
			ItemEdit.Slot = slot;
		}

		[RelayCommand]
		private void GoToTab(string tab)
		{
			if ((tab == "Personaje" || tab == "Libreria") ? true : false)
			{
				SelectedTabIndex = 1;
				PersonajeInnerTabIndex = 0;
				return;
			}
			if (1 == 0)
			{
			}
			int selectedTabIndex = tab switch
			{
				"Builds" => 2, 
				"Novedades" => 3, 
				"Exploracion" => 4, 
				"AcercaDe" => 5, 
				_ => 0, 
			};
			if (1 == 0)
			{
			}
			SelectedTabIndex = selectedTabIndex;
		}

		private void RequestPickForSlot(ItemSlotViewModel slot)
		{
			Library.PickTarget = slot;
			SelectSlot(slot);
			SelectedTabIndex = 1;
			PersonajeInnerTabIndex = 0;
		}

		public void LoadFromPath(string plrPath)
		{
			try
			{
				Library.PickTarget = null;
				ItemEdit.Slot = null;
				_loaded = _service.Load(plrPath);
				RebuildContainers();
				Appearance.LoadFrom(_loaded.Character);
				Servers.LoadFrom(_loaded.Character);
				Flags.LoadFrom(_loaded.Character);
				VersionEditor.LoadFrom(_loaded.Character);
				Buffs.LoadFrom(_loaded.Character);
				CharacterName = _loaded.Character.Name;
				HasCalamityData = _loaded.TplrPath != null;
				IsCharacterLoaded = true;
				int value = _loaded.MergedContainers.Values.Sum((GameItem[] items) => items.Count((GameItem i) => i.IsCalamity));
				StatusMessage = (HasCalamityData ? $"Cargado '{_loaded.Character.Name}' - {value} objeto(s) de Calamity detectado(s)." : ("Cargado '" + _loaded.Character.Name + "' - personaje 100% vanilla (sin .tplr)."));
			}
			catch (Exception ex)
			{
				IsCharacterLoaded = false;
				StatusMessage = "Error al cargar: " + ex.Message;
			}
		}

		[RelayCommand(CanExecute = "IsCharacterLoaded")]
		private void Save()
		{
			if (_loaded == null)
			{
				return;
			}
			try
			{
				SyncEditsBackToMerged();
				_service.Save(_loaded);
				StatusMessage = "Guardado: " + Path.GetFileName(_loaded.PlrPath) + ((_loaded.TplrPath != null) ? (" + " + Path.GetFileName(_loaded.TplrPath)) : "");
				SaveConfirmationVisible = true;
				_saveConfirmationTimer.Stop();
				_saveConfirmationTimer.Start();
			}
			catch (Exception ex)
			{
				StatusMessage = "Error al guardar: " + ex.Message;
			}
		}

		private void RebuildContainers()
		{
			Containers.Clear();
			Research.Clear();
			EquipmentGroup = null;
			if (_loaded != null)
			{
				AddContainer("inventory", "Inventario", _loaded.MergedContainers["inventory"]);
				AddContainer("bank", "Banco", _loaded.MergedContainers["bank"]);
				AddContainer("bank2", "Caja fuerte", _loaded.MergedContainers["bank2"]);
				AddContainer("bank3", "Fragua del Defensor", _loaded.MergedContainers["bank3"]);
				AddContainer("bank4", "Boveda del Vacio", _loaded.MergedContainers["bank4"]);
				AddContainer("miscEquips", "Mascota / Montura / Gancho", _loaded.MergedContainers["miscEquips"]);
				AddContainer("miscDyes", "Tintes (mascota/montura/gancho)", _loaded.MergedContainers["miscDyes"]);
				AddContainer("coins", "Monedas", _loaded.Character.Coins.ToGameItems());
				AddContainer("ammo", "Municion", _loaded.Character.Ammo.ToGameItems());
				EquipmentGroup = new EquipmentGroupViewModel(_service, RequestPickForSlot, _loaded.MergedContainers, _loaded.Character.Loadouts.Length);
				RebuildResearch();
			}
		}

		private void RebuildResearch()
		{
			Research.Clear();
			if (_loaded == null)
			{
				return;
			}
			foreach (PlrResearchEntry item in _loaded.Character.Research.OrderBy((PlrResearchEntry e) => e.Pid))
			{
				bool flag = item.Pid.Contains('/');
				string displayName;
				string iconPath;
				if (flag)
				{
					int num = item.Pid.IndexOf('/');
					string mod = item.Pid.Substring(0, num);
					string internalName = item.Pid.Substring(num + 1);
					CalamityCatalogEntry calamityCatalogEntry = _service.CalamityCatalog.ByModAndInternal(mod, internalName);
					displayName = calamityCatalogEntry?.DisplayName ?? item.Pid;
					iconPath = ((calamityCatalogEntry != null && calamityCatalogEntry.Icon != null) ? ("pack://siteoforigin:,,,/Assets/calamity/icons/" + calamityCatalogEntry.Icon) : null);
				}
				else
				{
					displayName = _service.VanillaCatalog.GetNameByKey(item.Pid);
					int? idByKey = _service.VanillaCatalog.GetIdByKey(item.Pid);
					iconPath = (idByKey.HasValue ? VanillaIconResolver.GetIconPath(idByKey.Value) : null);
				}
				Research.Add(new ResearchRowViewModel(displayName, item.Count, flag, iconPath));
			}
		}

		[RelayCommand(CanExecute = "IsCharacterLoaded")]
		private void ResearchAll()
		{
			if (_loaded == null)
			{
				return;
			}
			HashSet<string> hashSet = new HashSet<string>(_loaded.Character.Research.Select((PlrResearchEntry e) => e.Pid));
			foreach (string item in _service.VanillaCatalog.AllInternalNames())
			{
				if (hashSet.Add(item))
				{
					_loaded.Character.Research.Add(new PlrResearchEntry
					{
						Pid = item,
						Count = 9999
					});
				}
			}
			foreach (CalamityCatalogEntry entry in _service.CalamityCatalog.Entries)
			{
				string text = entry.Mod + "/" + entry.Internal;
				if (hashSet.Add(text))
				{
					_loaded.Character.Research.Add(new PlrResearchEntry
					{
						Pid = text,
						Count = 9999
					});
				}
			}
			RebuildResearch();
			StatusMessage = $"Investigacion completa aplicada ({Research.Count} objetos) - pulsa Guardar para conservarlo.";
		}

		[RelayCommand(CanExecute = "IsCharacterLoaded")]
		private void AutoEquip(BuildClassGear? gear)
		{
			if (_loaded == null || gear == null || EquipmentGroup == null)
			{
				return;
			}
			ObservableCollection<ItemSlotViewModel> slots = EquipmentGroup.EquippedItems.Slots;
			ObservableCollection<ItemSlotViewModel> slots2 = Containers.First((ContainerViewModel c) => c.Key == "inventory").Slots;
			int placed = 0;
			int skipped = 0;
			for (int num = 0; num < gear.Armor.Count && num < 3; num++)
			{
				PlaceInSlot(slots[num], gear.Armor[num]);
			}
			for (int num2 = 0; num2 < gear.Accessories.Count && num2 < 5; num2++)
			{
				PlaceInSlot(slots[3 + num2], gear.Accessories[num2]);
			}
			foreach (BuildItemRef weapon in gear.Weapons)
			{
				ItemSlotViewModel itemSlotViewModel = slots2.FirstOrDefault((ItemSlotViewModel s) => s.IsEmpty);
				if (itemSlotViewModel == null)
				{
					skipped++;
				}
				else
				{
					PlaceInSlot(itemSlotViewModel, weapon);
				}
			}
			StatusMessage = ((skipped > 0) ? $"Auto-equipar: {placed} objeto(s) colocado(s), {skipped} sin resolver o sin hueco libre - pulsa Guardar para conservarlo." : $"Auto-equipar: {placed} objeto(s) colocado(s) - pulsa Guardar para conservarlo.");
			SelectedTabIndex = 1;
			void PlaceInSlot(ItemSlotViewModel slot, BuildItemRef itemRef)
			{
				GameItem gameItem = BuildItemResolver.Resolve(itemRef, _service.VanillaCatalog, _service.CalamityCatalog, _service.VanillaPrefixCatalog);
				if (gameItem == null)
				{
					skipped++;
				}
				else
				{
					slot.UpdateFrom(gameItem);
					placed++;
				}
			}
		}

		private void AddContainer(string key, string displayName, GameItem[] items)
		{
			ObservableCollection<ItemSlotViewModel> observableCollection = new ObservableCollection<ItemSlotViewModel>();
			for (int i = 0; i < items.Length; i++)
			{
				observableCollection.Add(new ItemSlotViewModel(_service, i, displayName, items[i], RequestPickForSlot));
			}
			Containers.Add(new ContainerViewModel(key, displayName, observableCollection));
		}

		private void SyncEditsBackToMerged()
		{
			if (_loaded != null)
			{
				SyncContainersBackToMerged(Containers);
				if (EquipmentGroup != null)
				{
					SyncContainersBackToMerged(EquipmentGroup.AllContainers);
				}
			}
		}

		private void SyncContainersBackToMerged(IEnumerable<ContainerViewModel> containers)
		{
			if (_loaded == null)
			{
				return;
			}
			foreach (ContainerViewModel container in containers)
			{
				GameItem[] value;
				if (container.Key == "coins")
				{
					CopySlotsInto(_loaded.Character.Coins, container.Slots);
				}
				else if (container.Key == "ammo")
				{
					CopySlotsInto(_loaded.Character.Ammo, container.Slots);
				}
				else if (_loaded.MergedContainers.TryGetValue(container.Key, out value))
				{
					for (int i = 0; i < container.Slots.Count && i < value.Length; i++)
					{
						value[i] = container.Slots[i].Item;
					}
				}
			}
		}

		private static void CopySlotsInto(PlrItemSlot[] target, IReadOnlyList<ItemSlotViewModel> source)
		{
			PlrItemSlot[] array = source.Select((ItemSlotViewModel s) => s.Item).ToArray().ToPlrItemSlots();
			for (int num = 0; num < target.Length && num < array.Length; num++)
			{
				target[num] = array[num];
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnIsCharacterLoadedChanged(bool value)
		{
			SaveCommand.NotifyCanExecuteChanged();
			ResearchAllCommand.NotifyCanExecuteChanged();
			AutoEquipCommand.NotifyCanExecuteChanged();
		}
	}
	public sealed class PrefixCatalogEntryViewModel(string displayName, ItemPrefix prefix, bool isCalamity, bool isCurrent = false)
	{
		public string DisplayName { get; } = displayName;

		public ItemPrefix Prefix { get; } = prefix;

		public bool IsCalamity { get; } = isCalamity;

		public bool IsCurrent { get; } = isCurrent;
	}
	public class PrefixGroupButtonViewModel(PrefixGroup group) : ObservableObject
	{
		[ObservableProperty]
		private bool _isSelected;

		public PrefixGroup Group { get; } = group;

		public string Label { get; } = group.NameEs;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool IsSelected
		{
			get
			{
				return _isSelected;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_isSelected, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsSelected);
					_isSelected = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsSelected);
				}
			}
		}
	}
	public class PrefixMetaButtonViewModel(PrefixMeta meta) : ObservableObject
	{
		[ObservableProperty]
		private bool _isSelected;

		public PrefixMeta Meta { get; } = meta;

		public string Label { get; } = meta.NameEs;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public bool IsSelected
		{
			get
			{
				return _isSelected;
			}
			set
			{
				if (!EqualityComparer<bool>.Default.Equals(_isSelected, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsSelected);
					_isSelected = value;
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsSelected);
				}
			}
		}
	}
	public sealed class ResearchRowViewModel(string displayName, int count, bool isCalamity, string? iconPath)
	{
		public string DisplayName { get; } = displayName;

		public int Count { get; } = count;

		public bool IsCalamity { get; } = isCalamity;

		public string? IconPath { get; } = iconPath;
	}
	public class ServerEntryRowViewModel : ObservableObject
	{
		private bool _suppressWriteback;

		[ObservableProperty]
		private string _name;

		[ObservableProperty]
		private int _spawnX;

		[ObservableProperty]
		private int _spawnY;

		[ObservableProperty]
		private int _address;

		public PlrServerEntry Entry { get; }

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public string Name
		{
			get
			{
				return _name;
			}
			[MemberNotNull("_name")]
			set
			{
				if (!EqualityComparer<string>.Default.Equals(_name, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Name);
					_name = value;
					OnNameChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Name);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public int SpawnX
		{
			get
			{
				return _spawnX;
			}
			set
			{
				if (!EqualityComparer<int>.Default.Equals(_spawnX, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SpawnX);
					_spawnX = value;
					OnSpawnXChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SpawnX);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public int SpawnY
		{
			get
			{
				return _spawnY;
			}
			set
			{
				if (!EqualityComparer<int>.Default.Equals(_spawnY, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SpawnY);
					_spawnY = value;
					OnSpawnYChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SpawnY);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public int Address
		{
			get
			{
				return _address;
			}
			set
			{
				if (!EqualityComparer<int>.Default.Equals(_address, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Address);
					_address = value;
					OnAddressChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Address);
				}
			}
		}

		public ServerEntryRowViewModel(PlrServerEntry entry)
		{
			Entry = entry;
			_suppressWriteback = true;
			_name = entry.Name;
			SpawnX = entry.SpawnX;
			SpawnY = entry.SpawnY;
			Address = entry.Address;
			_suppressWriteback = false;
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnNameChanged(string value)
		{
			if (!_suppressWriteback)
			{
				Entry.Name = value;
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnSpawnXChanged(int value)
		{
			if (!_suppressWriteback)
			{
				Entry.SpawnX = value;
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnSpawnYChanged(int value)
		{
			if (!_suppressWriteback)
			{
				Entry.SpawnY = value;
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnAddressChanged(int value)
		{
			if (!_suppressWriteback)
			{
				Entry.Address = value;
			}
		}
	}
	public class ServersViewModel : ObservableObject
	{
		private PlrCharacter? _character;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand? addEntryCommand;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand<ServerEntryRowViewModel?>? removeEntryCommand;

		public ObservableCollection<ServerEntryRowViewModel> Entries { get; } = new ObservableCollection<ServerEntryRowViewModel>();

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand AddEntryCommand => addEntryCommand ?? (addEntryCommand = new RelayCommand(AddEntry));

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand<ServerEntryRowViewModel?> RemoveEntryCommand => removeEntryCommand ?? (removeEntryCommand = new RelayCommand<ServerEntryRowViewModel>(RemoveEntry));

		public void LoadFrom(PlrCharacter character)
		{
			_character = character;
			Entries.Clear();
			foreach (PlrServerEntry server in character.Servers)
			{
				Entries.Add(new ServerEntryRowViewModel(server));
			}
		}

		[RelayCommand]
		private void AddEntry()
		{
			if (_character != null)
			{
				PlrServerEntry plrServerEntry = new PlrServerEntry
				{
					SpawnX = 0,
					SpawnY = 0,
					Address = 0,
					Name = "Nuevo spawn point"
				};
				_character.Servers.Add(plrServerEntry);
				Entries.Add(new ServerEntryRowViewModel(plrServerEntry));
			}
		}

		[RelayCommand]
		private void RemoveEntry(ServerEntryRowViewModel? row)
		{
			if (_character != null && row != null)
			{
				_character.Servers.Remove(row.Entry);
				Entries.Remove(row);
			}
		}
	}
	public sealed record VersionOption(string Label, int Number);
	public sealed record VersionGroup(string Label, IReadOnlyList<VersionOption> Options);
	public class VersionEditorViewModel : ObservableObject
	{
		private PlrCharacter? _character;

		private bool _suppressWriteback;

		[ObservableProperty]
		private int _rawVersion;

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		private RelayCommand<int>? setVersionCommand;

		public IReadOnlyList<VersionGroup> Groups { get; } = new global::<>z__ReadOnlyArray<VersionGroup>(new VersionGroup[4]
		{
			new VersionGroup("1.1.x", new global::<>z__ReadOnlySingleElementList<VersionOption>(new VersionOption("1.1.2", 39))),
			new VersionGroup("1.2.x", new global::<>z__ReadOnlyArray<VersionOption>(new VersionOption[5]
			{
				new VersionOption("1.2.0", 69),
				new VersionOption("1.2.1", 73),
				new VersionOption("1.2.2", 77),
				new VersionOption("1.2.3", 93),
				new VersionOption("1.2.4", 98)
			})),
			new VersionGroup("1.3.x", new global::<>z__ReadOnlyArray<VersionOption>(new VersionOption[5]
			{
				new VersionOption("1.3.0", 145),
				new VersionOption("1.3.1", 168),
				new VersionOption("1.3.3", 175),
				new VersionOption("1.3.4", 184),
				new VersionOption("1.3.5", 190)
			})),
			new VersionGroup("1.4.x", new global::<>z__ReadOnlyArray<VersionOption>(new VersionOption[6]
			{
				new VersionOption("1.4.0", 225),
				new VersionOption("1.4.0.5", 230),
				new VersionOption("1.4.1.2", 237),
				new VersionOption("1.4.3.0", 248),
				new VersionOption("1.4.4.0", 269),
				new VersionOption("1.4.5.0 / 1.4.5.x", 315)
			}))
		});

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public int RawVersion
		{
			get
			{
				return _rawVersion;
			}
			set
			{
				if (!EqualityComparer<int>.Default.Equals(_rawVersion, value))
				{
					OnPropertyChanging(__KnownINotifyPropertyChangingArgs.RawVersion);
					_rawVersion = value;
					OnRawVersionChanged(value);
					OnPropertyChanged(__KnownINotifyPropertyChangedArgs.RawVersion);
				}
			}
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
		[ExcludeFromCodeCoverage]
		public IRelayCommand<int> SetVersionCommand => setVersionCommand ?? (setVersionCommand = new RelayCommand<int>(SetVersion));

		public void LoadFrom(PlrCharacter character)
		{
			_character = null;
			_suppressWriteback = true;
			RawVersion = character.Version;
			_suppressWriteback = false;
			_character = character;
		}

		[RelayCommand]
		private void SetVersion(int number)
		{
			RawVersion = number;
		}

		[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
		private void OnRawVersionChanged(int value)
		{
			if (!_suppressWriteback && _character != null)
			{
				_character.Version = value;
			}
		}
	}
	public sealed class WhatsNewViewModel(WhatsNewCatalog catalog)
	{
		public IReadOnlyList<WhatsNewEntry> Entries { get; } = catalog.Entries;
	}
}
namespace TerrasavrNative.App.Services
{
	public sealed class LoadedCharacter(string plrPath, string? tplrPath, string tplrRootName, PlrCharacter character, NbtCompound? tplrRoot, Dictionary<string, GameItem[]> mergedContainers)
	{
		public string PlrPath { get; } = plrPath;

		public string? TplrPath { get; set; } = tplrPath;

		public string TplrRootName { get; } = tplrRootName;

		public PlrCharacter Character { get; } = character;

		public NbtCompound? TplrRoot { get; set; } = tplrRoot;

		public Dictionary<string, GameItem[]> MergedContainers { get; } = mergedContainers;
	}
	public sealed class CharacterFileService
	{
		private readonly CalamityCharacterSync _sync;

		public CalamityCatalog CalamityCatalog { get; }

		public CalamityBuffCatalog CalamityBuffCatalog { get; }

		public RoguePrefixCatalog RoguePrefixCatalog { get; }

		public VanillaItemCatalog VanillaCatalog { get; }

		public VanillaPrefixCatalog VanillaPrefixCatalog { get; }

		public BuildsCatalog VanillaBuilds { get; }

		public BuildsCatalog CalamityBuilds { get; }

		public WhatsNewCatalog WhatsNew { get; }

		public ChangelogCatalog Changelog { get; }

		public MapColorCatalog MapColors { get; }

		public TileNameCatalog TileNames { get; }

		public NpcNameCatalog NpcNames { get; }

		public VanillaBuffCatalog VanillaBuffs { get; }

		public BestPrefixCatalog BestPrefixes { get; }

		public VanillaCategoryCatalog VanillaCategories { get; }

		public VanillaItemStatsCatalog VanillaStats { get; }

		public PrefixRulesCatalog PrefixRules { get; }

		public HairDyeCatalog HairDyes { get; }

		public CharacterFileService()
		{
			string path = Path.Combine(AppContext.BaseDirectory, "Assets");
			CalamityCatalog = TerrasavrNative.Core.Data.CalamityCatalog.LoadFromFile(Path.Combine(path, "calamity", "catalog.json"));
			CalamityBuffCatalog = TerrasavrNative.Core.Data.CalamityBuffCatalog.LoadFromFile(Path.Combine(path, "calamity", "buffs.json"), Path.Combine(path, "calamity_buff_descriptions.json"));
			RoguePrefixCatalog = TerrasavrNative.Core.Data.RoguePrefixCatalog.LoadFromFile(Path.Combine(path, "calamity", "rogue_prefixes.json"));
			VanillaCatalog = VanillaItemCatalog.LoadFromFile(Path.Combine(path, "vanilla_item_names.json"), Path.Combine(path, "vanilla_item_names_by_key.json"), Path.Combine(path, "vanilla_item_ids_by_key.json"));
			VanillaPrefixCatalog = TerrasavrNative.Core.Data.VanillaPrefixCatalog.LoadFromFile(Path.Combine(path, "calamity", "prefixes.json"));
			VanillaBuilds = BuildsCatalog.LoadFromFile(Path.Combine(path, "builds.json"));
			CalamityBuilds = BuildsCatalog.LoadFromFile(Path.Combine(path, "builds_calamity.json"));
			WhatsNew = WhatsNewCatalog.LoadFromFile(Path.Combine(path, "whats_new.json"));
			Changelog = ChangelogCatalog.LoadFromFile(Path.Combine(path, "changelog.json"));
			MapColors = MapColorCatalog.LoadFromFile(Path.Combine(path, "map_colors.json"));
			TileNames = TileNameCatalog.LoadFromFile(Path.Combine(path, "tile_names.json"));
			NpcNames = NpcNameCatalog.LoadFromFile(Path.Combine(path, "npc_names.json"));
			VanillaBuffs = VanillaBuffCatalog.LoadFromFile(Path.Combine(path, "vanilla_buff_names.json"), Path.Combine(path, "vanilla_buff_descriptions.json"));
			BestPrefixes = BestPrefixCatalog.LoadFromFile(Path.Combine(path, "calamity", "best_prefix.json"));
			VanillaCategories = VanillaCategoryCatalog.LoadFromFile(Path.Combine(path, "vanilla_categories.json"));
			VanillaStats = VanillaItemStatsCatalog.LoadFromFile(Path.Combine(path, "vanilla_stats.json"));
			PrefixRules = PrefixRulesCatalog.LoadFromFile(Path.Combine(path, "vanilla_prefix_rules.json"));
			HairDyes = HairDyeCatalog.LoadFromFile(Path.Combine(path, "hair_dyes.json"));
			CalamityPrefixTranslator prefixTranslator = new CalamityPrefixTranslator(RoguePrefixCatalog);
			CalamityItemCodec itemCodec = new CalamityItemCodec(CalamityCatalog, prefixTranslator);
			_sync = new CalamityCharacterSync(itemCodec, CalamityBuffCatalog);
		}

		public LoadedCharacter Load(string plrPath)
		{
			PlrCharacter character = PlrFile.Read(File.ReadAllBytes(plrPath));
			string text = Path.ChangeExtension(plrPath, ".tplr");
			NbtCompound tplrRoot = null;
			string tplrRootName = "Player";
			bool flag = File.Exists(text);
			if (flag)
			{
				(tplrRootName, tplrRoot) = TplrFile.Read(File.ReadAllBytes(text));
			}
			Dictionary<string, GameItem[]> mergedContainers = _sync.MergeAll(character, tplrRoot);
			return new LoadedCharacter(plrPath, flag ? text : null, tplrRootName, character, tplrRoot, mergedContainers);
		}

		public void Save(LoadedCharacter loaded)
		{
			NbtCompound nbtCompound = _sync.MaskAndSyncAll(loaded.Character, loaded.MergedContainers, loaded.TplrRoot);
			File.WriteAllBytes(loaded.PlrPath, PlrFile.Write(loaded.Character));
			string text = loaded.TplrPath ?? Path.ChangeExtension(loaded.PlrPath, ".tplr");
			File.WriteAllBytes(text, TplrFile.Write(loaded.TplrRootName, nbtCompound));
			loaded.TplrRoot = nbtCompound;
			loaded.TplrPath = text;
		}
	}
	public static class NpcIconResolver
	{
		private static readonly string IconsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "npc_icons");

		public static string? GetIconPath(int npcId)
		{
			string path = Path.Combine(IconsDir, $"{npcId}.png");
			return File.Exists(path) ? ("pack://siteoforigin:,,,/Assets/npc_icons/" + npcId + ".png") : null;
		}
	}
	public static class PlayerPreviewRenderer
	{
		public readonly record struct Tint(byte R, byte G, byte B);

		public readonly record struct PlayerColors(Tint Hair, Tint Skin, Tint Eyes, Tint Shirt, Tint Under, Tint Pants, Tint Shoes);

		private const int Width = 40;

		private const int Height = 56;

		private const int HairStyleMin = 1;

		private const int HairStyleMax = 228;

		private static readonly Dictionary<string, byte[]> Cache = new Dictionary<string, byte[]>();

		public static int HairStyleCount => 228;

		public static WriteableBitmap Render(int hairStyle, bool isMale, PlayerColors colors)
		{
			//IL_0151: Unknown result type (might be due to invalid IL or missing references)
			byte[] array = new byte[8960];
			Composite(array, LoadBody("legskin"), colors.Skin);
			Composite(array, LoadBody("pants"), colors.Pants);
			Composite(array, LoadBody("shoes"), colors.Shoes);
			Composite(array, LoadBody("torsoskin"), colors.Skin);
			Composite(array, LoadBody("undershirt"), colors.Under);
			Composite(array, LoadBody("shirt"), colors.Shirt);
			Composite(array, LoadBody("head"), colors.Skin);
			Composite(array, LoadHair(hairStyle), colors.Hair);
			Composite(array, LoadBody("eyewhites"), null);
			Composite(array, LoadBody("eyes"), colors.Eyes);
			WriteableBitmap writeableBitmap = new WriteableBitmap(40, 56, 96.0, 96.0, PixelFormats.Bgra32, null);
			writeableBitmap.WritePixels(new Int32Rect(0, 0, 40, 56), array, 160, 0);
			((Freezable)writeableBitmap).Freeze();
			return writeableBitmap;
		}

		public static WriteableBitmap RenderHairThumbnail(int hairStyle, Tint hairColor)
		{
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			byte[] array = new byte[8960];
			Composite(array, LoadHair(hairStyle), hairColor);
			WriteableBitmap writeableBitmap = new WriteableBitmap(40, 56, 96.0, 96.0, PixelFormats.Bgra32, null);
			writeableBitmap.WritePixels(new Int32Rect(0, 0, 40, 56), array, 160, 0);
			((Freezable)writeableBitmap).Freeze();
			return writeableBitmap;
		}

		private static byte[] LoadBody(string name)
		{
			InlineArray5<string> buffer = default(InlineArray5<string>);
			buffer[0] = AppContext.BaseDirectory;
			buffer[1] = "Assets";
			buffer[2] = "player";
			buffer[3] = "body";
			buffer[4] = name + ".png";
			return LoadCached(Path.Combine(buffer));
		}

		private static byte[] LoadHair(int hairStyle)
		{
			int num = Math.Clamp(hairStyle, 1, 228);
			InlineArray5<string> buffer = default(InlineArray5<string>);
			buffer[0] = AppContext.BaseDirectory;
			buffer[1] = "Assets";
			buffer[2] = "player";
			buffer[3] = "hair";
			buffer[4] = num + ".png";
			string path = Path.Combine(buffer);
			if (!File.Exists(path))
			{
				InlineArray5<string> buffer2 = default(InlineArray5<string>);
				buffer2[0] = AppContext.BaseDirectory;
				buffer2[1] = "Assets";
				buffer2[2] = "player";
				buffer2[3] = "hair";
				buffer2[4] = "1.png";
				path = Path.Combine(buffer2);
			}
			return LoadCached(path);
		}

		private static byte[] LoadCached(string path)
		{
			if (Cache.TryGetValue(path, out byte[] value))
			{
				return value;
			}
			byte[] array = LoadPngPixels(path);
			Cache[path] = array;
			return array;
		}

		private static byte[] LoadPngPixels(string path)
		{
			PngBitmapDecoder pngBitmapDecoder = new PngBitmapDecoder(new Uri(path), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
			FormatConvertedBitmap formatConvertedBitmap = new FormatConvertedBitmap(pngBitmapDecoder.Frames[0], PixelFormats.Bgra32, null, 0.0);
			byte[] array = new byte[8960];
			formatConvertedBitmap.CopyPixels(array, 160, 0);
			return array;
		}

		private static void Composite(byte[] dst, byte[] src, Tint? tint)
		{
			for (int i = 0; i < src.Length; i += 4)
			{
				byte b = src[i];
				byte b2 = src[i + 1];
				byte b3 = src[i + 2];
				byte b4 = src[i + 3];
				if (b4 == 0)
				{
					continue;
				}
				if (tint.HasValue)
				{
					Tint valueOrDefault = tint.GetValueOrDefault();
					if (true)
					{
						b3 = (byte)(b3 * valueOrDefault.R / 255);
						b2 = (byte)(b2 * valueOrDefault.G / 255);
						b = (byte)(b * valueOrDefault.B / 255);
					}
				}
				float num = (float)(int)b4 / 255f;
				dst[i] = (byte)((float)(int)b * num + (float)(int)dst[i] * (1f - num));
				dst[i + 1] = (byte)((float)(int)b2 * num + (float)(int)dst[i + 1] * (1f - num));
				dst[i + 2] = (byte)((float)(int)b3 * num + (float)(int)dst[i + 2] * (1f - num));
				dst[i + 3] = (byte)((float)(int)b4 + (float)(int)dst[i + 3] * (1f - num));
			}
		}
	}
	public static class VanillaBuffIconResolver
	{
		private static readonly string IconsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "vanilla", "buff_icons");

		public static string? GetIconPath(int buffId)
		{
			string path = Path.Combine(IconsDir, $"{buffId}.png");
			return File.Exists(path) ? ("pack://siteoforigin:,,,/Assets/vanilla/buff_icons/" + buffId + ".png") : null;
		}
	}
	public static class VanillaIconResolver
	{
		private static readonly string IconsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "vanilla", "icons");

		public static string? GetIconPath(int itemId)
		{
			string path = Path.Combine(IconsDir, $"{itemId}.png");
			return File.Exists(path) ? ("pack://siteoforigin:,,,/Assets/vanilla/icons/" + itemId + ".png") : null;
		}
	}
	public static class WorldRenderer
	{
		private static readonly Color FallbackBackgroundColor = Color.FromRgb(18, 18, 18);

		public static WriteableBitmap Render(WldWorld world, MapColorCatalog colors)
		{
			//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
			int tilesWide = world.Header.TilesWide;
			int tilesHigh = world.Header.TilesHigh;
			WriteableBitmap writeableBitmap = new WriteableBitmap(tilesWide, tilesHigh, 96.0, 96.0, PixelFormats.Bgra32, null);
			int num = tilesWide * 4;
			byte[] array = new byte[tilesHigh * num];
			(byte, byte, byte, byte)[] array2 = new(byte, byte, byte, byte)[tilesHigh];
			for (int i = 0; i < tilesHigh; i++)
			{
				RgbaColor rgbaColor = colors.Global(world.Header.ZoneFor(i));
				array2[i] = ((rgbaColor.A > 0) ? (rgbaColor.R, rgbaColor.G, rgbaColor.B, byte.MaxValue) : Blend(FallbackBackgroundColor));
			}
			for (int j = 0; j < tilesWide; j++)
			{
				for (int k = 0; k < tilesHigh; k++)
				{
					WldTile wldTile = world.Tiles[j, k];
					(byte, byte, byte, byte) bg = array2[k];
					if (wldTile.Wall != 0)
					{
						RgbaColor fg = colors.WallColor(wldTile.Wall);
						if (fg.A > 0)
						{
							bg = Blend(bg, fg);
						}
					}
					if (wldTile.IsActive)
					{
						RgbaColor fg2 = colors.TileColor(wldTile.Type);
						if (fg2.A > 0)
						{
							bg = Blend(bg, fg2);
						}
					}
					if (wldTile.LiquidAmount > 0)
					{
						(byte, byte, byte, byte) fg3 = LiquidColor(wldTile.LiquidType);
						bg = Blend(bg, fg3);
					}
					int num2 = k * num + j * 4;
					array[num2] = bg.Item3;
					array[num2 + 1] = bg.Item2;
					array[num2 + 2] = bg.Item1;
					array[num2 + 3] = byte.MaxValue;
				}
			}
			writeableBitmap.WritePixels(new Int32Rect(0, 0, tilesWide, tilesHigh), array, num, 0);
			((Freezable)writeableBitmap).Freeze();
			return writeableBitmap;
		}

		private static (byte R, byte G, byte B, byte A) LiquidColor(byte liquidType)
		{
			if (1 == 0)
			{
			}
			(byte, byte, byte, byte) result = liquidType switch
			{
				2 => (250, 100, 0, 200), 
				3 => (200, 170, byte.MaxValue, 200), 
				_ => (30, 110, 220, 160), 
			};
			if (1 == 0)
			{
			}
			return result;
		}

		private static (byte R, byte G, byte B, byte A) Blend(Color c)
		{
			return (R: c.R, G: c.G, B: c.B, A: byte.MaxValue);
		}

		private static (byte R, byte G, byte B, byte A) Blend((byte R, byte G, byte B, byte A) bg, RgbaColor fg)
		{
			float num = (float)(int)fg.A / 255f;
			return (R: (byte)((float)(int)fg.R * num + (float)(int)bg.R * (1f - num)), G: (byte)((float)(int)fg.G * num + (float)(int)bg.G * (1f - num)), B: (byte)((float)(int)fg.B * num + (float)(int)bg.B * (1f - num)), A: byte.MaxValue);
		}

		private static (byte R, byte G, byte B, byte A) Blend((byte R, byte G, byte B, byte A) bg, (byte R, byte G, byte B, byte A) fg)
		{
			float num = (float)(int)fg.A / 255f;
			return (R: (byte)((float)(int)fg.R * num + (float)(int)bg.R * (1f - num)), G: (byte)((float)(int)fg.G * num + (float)(int)bg.G * (1f - num)), B: (byte)((float)(int)fg.B * num + (float)(int)bg.B * (1f - num)), A: byte.MaxValue);
		}
	}
}
namespace TerrasavrNative.App.Converters
{
	public sealed class NullToVisibilityConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
		{
			return (value == null) ? Visibility.Collapsed : Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
	public sealed class NullToCollapsedConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
		{
			return (value != null) ? Visibility.Collapsed : Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
	public sealed class EmptyToCollapsedConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
		{
			return string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
	public sealed class CountToVisibilityConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
		{
			return (!(value is int num) || num <= 0) ? Visibility.Collapsed : Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
	public sealed class InverseBooleanToVisibilityConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
		{
			return (value is bool && (bool)value) ? Visibility.Collapsed : Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
