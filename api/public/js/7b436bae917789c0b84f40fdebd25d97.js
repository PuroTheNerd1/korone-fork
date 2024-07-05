var _____WB$wombat$assign$function_____ = function(name) {return (self._wb_wombat && self._wb_wombat.local_init && self._wb_wombat.local_init(name)) || self[name]; };
if (!self.__WB_pmw) { self.__WB_pmw = function(obj) { this.__WB_source = obj; return this; } }
{
  let window = _____WB$wombat$assign$function_____("window");
  let self = _____WB$wombat$assign$function_____("self");
  let document = _____WB$wombat$assign$function_____("document");
  let location = _____WB$wombat$assign$function_____("location");
  let top = _____WB$wombat$assign$function_____("top");
  let parent = _____WB$wombat$assign$function_____("parent");
  let frames = _____WB$wombat$assign$function_____("frames");
  let opener = _____WB$wombat$assign$function_____("opener");

;// bundle: Widgets___DropdownMenu___1b78138201eaf5ac3f85361ff82eddc2_m
;// files: modules/Widgets/DropdownMenu.js

;// modules/Widgets/DropdownMenu.js
Roblox.define("Widgets.DropdownMenu",[],function(){function t(n){$(n).on("click",".button",function(){var n=$(this),i,t;return n.hasClass("init")||(i=$(this).outerWidth()-parseInt(n.css("border-left-width"))-parseInt(n.css("border-right-width")),n.siblings(".dropdown-list").css("min-width",i),t=n.siblings('.dropdown-list[data-align="right"]').first(),t.css("right",0),n.addClass("init")),n.hasClass("active")?(n.removeClass("active"),n.siblings(".dropdown-list").hide()):(n.addClass("active"),n.siblings(".dropdown-list").show()),$(document).click(function(){$(".button.init.active").removeClass("active"),$(".dropdown-list").hide()}),!1})}function n(){var n=$(".button").not(".init");n.each(function(){var t=$(this).outerWidth()-parseInt($(this).css("border-left-width"))-parseInt($(this).css("border-right-width")),n;$(this).siblings(".dropdown-list").css("min-width",t),n=$(this).siblings('.dropdown-list[data-align="right"]').first(),n.css("right",0)}),$(".dropdown-list").hide(),n.click(function(){return $(this).hasClass("active")?($(this).removeClass("active"),$(this).siblings(".dropdown-list").hide()):($(this).addClass("active"),$(this).siblings(".dropdown-list").show()),!1}),$(document).click(function(){n.removeClass("active"),$(".dropdown-list").hide()}),n.addClass("init")}return{InitializeDropdown:n,LazyInitializeDropdown:t}});


}
/*
     FILE ARCHIVED ON 07:33:03 Jan 29, 2016 AND RETRIEVED FROM THE
     INTERNET ARCHIVE ON 07:33:46 Jul 05, 2024.
     JAVASCRIPT APPENDED BY WAYBACK MACHINE, COPYRIGHT INTERNET ARCHIVE.

     ALL OTHER CONTENT MAY ALSO BE PROTECTED BY COPYRIGHT (17 U.S.C.
     SECTION 108(a)(3)).
*/
/*
playback timings (ms):
  captures_list: 0.99
  exclusion.robots: 0.141
  exclusion.robots.policy: 0.127
  esindex: 0.014
  cdx.remote: 25.187
  LoadShardBlock: 91.017 (3)
  PetaboxLoader3.datanode: 3537.06 (5)
  load_resource: 4686.946 (2)
  PetaboxLoader3.resolve: 1137.287 (2)
*/