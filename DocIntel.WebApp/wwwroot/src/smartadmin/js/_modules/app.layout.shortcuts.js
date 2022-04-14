var layouts = (function(setlayout) {

	setlayout.errorMessage = function(layout){
		
		console.log("('" + layout + "')" +" is not a valid entry, enter 'on' or 'off'");
	}

	setlayout.fixedHeader = function(layout) {
		if (layout === 'on') {
			initApp.pushSettings('header-function-fixed')
		} else if (layout === 'off') {
			initApp.removeSettings('header-function-fixed')
		} else {
			layouts.errorMessage(layout);
		}
	};
	setlayout.fixedNavigation = function(layout) {
		if (layout === 'on') {
			initApp.pushSettings('nav-function-fixed')
		} else if (layout === 'off') {
			initApp.removeSettings('nav-function-fixed')
		} else {
			layouts.errorMessage(layout);
		}
	};
	setlayout.minifyNavigation = function(layout) {
		if (layout === 'on') {
			initApp.pushSettings('nav-function-minify')
		} else if (layout === 'off') {
			initApp.removeSettings('nav-function-minify')
		} else {
			layouts.errorMessage(layout);
		}
	};
	setlayout.hideNavigation = function(layout) {
		if (layout === 'on') {
			initApp.pushSettings('nav-function-hidden')
		} else if (layout === 'off') {
			initApp.removeSettings('nav-function-hidden')
		} else {
			layouts.errorMessage(layout);
		}
	};
	setlayout.horizontalNavigation = function(layout) {
		if (layout === 'on') {
			initApp.pushSettings('nav-function-top')
		} else if (layout === 'off') {
			initApp.removeSettings('nav-function-top')
		} else {
			layouts.errorMessage(layout);
		}
	};
	setlayout.fixedFooter = function(layout) {
		if (layout === 'on') {
			initApp.pushSettings('footer-function-fixed')
		} else if (layout === 'off') {
			initApp.removeSettings('footer-function-fixed')
		} else {
			layouts.errorMessage(layout);
		}
	};
	setlayout.boxed = function(layout) {
		if (layout === 'on') {
			initApp.pushSettings('mod-main-boxed')
		} else if (layout === 'off') {
			initApp.removeSettings('mod-main-boxed')
		} else {
			layouts.errorMessage(layout);
		}
	};
	//mobile
	setlayout.pushContent = function(layout) {
		if (layout === 'on') {
			initApp.pushSettings('nav-mobile-push')
		} else if (layout === 'off') {
			initApp.removeSettings('nav-mobile-push')
		} else {
			layouts.errorMessage(layout);
		}
	};
	setlayout.overlay = function(layout) {
		if (layout === 'on') {
			initApp.pushSettings('nav-mobile-no-overlay')
		} else if (layout === 'off') {
			initApp.removeSettings('nav-mobile-no-overlay')
		} else {
			layouts.errorMessage(layout);
		}
	};
	setlayout.offCanvas = function(layout) {
		if (layout === 'on') {
			initApp.pushSettings('nav-mobile-slide-out')
		} else if (layout === 'off') {
			initApp.removeSettings('nav-mobile-slide-out')
		} else {
			layouts.errorMessage(layout);
		}
	};
	//accessibility
	setlayout.bigFonts = function(layout) {
		if (layout === 'on') {
			initApp.pushSettings('mod-bigger-font')
		} else if (layout === 'off') {
			initApp.removeSettings('mod-bigger-font')
		} else {
			layouts.errorMessage(layout);
		}
	};
	setlayout.highContrast = function(layout) {
		if (layout === 'on') {
			initApp.pushSettings('mod-high-contrast')
		} else if (layout === 'off') {
			initApp.removeSettings('mod-high-contrast')
		} else {
			layouts.errorMessage(layout);
		}
	};
	setlayout.colorblind = function(layout) {
		if (layout === 'on') {
			initApp.pushSettings('mod-color-blind')
		} else if (layout === 'off') {
			initApp.removeSettings('mod-color-blind')
		} else {
			layouts.errorMessage(layout);
		}
	};
	setlayout.preloadInside = function(layout) {
		if (layout === 'on') {
			initApp.pushSettings('mod-pace-custom')
		} else if (layout === 'off') {
			initApp.removeSettings('mod-pace-custom')
		} else {
			layouts.errorMessage(layout);
		}
	};
	setlayout.panelIcons = function(layout) {
		if (layout === 'on') {
			initApp.pushSettings('mod-panel-icon')
		} else if (layout === 'off') {
			initApp.removeSettings('mod-panel-icon')
		} else {
			layouts.errorMessage(layout);
		}
	};
	//global
	setlayout.cleanBackground = function(layout) {
		if (layout === 'on') {
			initApp.pushSettings('mod-clean-page-bg')
		} else if (layout === 'off') {
			initApp.removeSettings('mod-clean-page-bg')
		} else {
			layouts.errorMessage(layout);
		}
	};
	setlayout.hideNavIcons = function(layout) {
		if (layout === 'on') {
			initApp.pushSettings('mod-hide-nav-icons')
		} else if (layout === 'off') {
			initApp.removeSettings('mod-hide-nav-icons')
		} else {
			layouts.errorMessage(layout);
		}
	};
	setlayout.noAnimation = function(layout) {
		if (layout === 'on') {
			initApp.pushSettings('mod-disable-animation')
		} else if (layout === 'off') {
			initApp.removeSettings('mod-disable-animation')
		} else {
			layouts.errorMessage(layout);
		}
	};
	setlayout.hideInfoCard = function(layout) {
		if (layout === 'on') {
			initApp.pushSettings('mod-hide-info-card')
		} else if (layout === 'off') {
			initApp.removeSettings('mod-hide-info-card')
		} else {
			layouts.errorMessage(layout);
		}
	};
	setlayout.leanSubheader = function(layout) {
		if (layout === 'on') {
			initApp.pushSettings('mod-lean-subheader')
		} else if (layout === 'off') {
			initApp.removeSettings('mod-lean-subheader')
		} else {
			layouts.errorMessage(layout);
		}
	};
	setlayout.hierarchicalNav = function(layout) {
		if (layout === 'on') {
			initApp.pushSettings('mod-nav-link')
		} else if (layout === 'off') {
			initApp.removeSettings('mod-nav-link')
		} else {
			layouts.errorMessage(layout);
		}
	};
	setlayout.darkNav = function(layout) {
		if (layout === 'on') {
			initApp.pushSettings('mod-nav-dark')
		} else if (layout === 'off') {
			initApp.removeSettings('mod-nav-dark')
		} else {
			layouts.errorMessage(layout);
		}
	};
	// ALT Approach
	/*setlayout.modeDark = function(layout) {
		if (layout === 'on') {
			initApp.pushSettings('mod-skin-dark')
		} else if (layout === 'off') {
			initApp.removeSettings('mod-skin-dark')
		} else {
			layouts.errorMessage(layout);
		}
	};
	setlayout.modeLight = function(layout) {
		if (layout === 'on') {
			initApp.pushSettings('mod-skin-light')
		} else if (layout === 'off') {
			initApp.removeSettings('mod-skin-light')
		} else {
			layouts.errorMessage(layout);
		}
	};
	setlayout.modeDefault = function() {
		layouts.modeDark('off');
		layouts.modeLight('off');
	};*/

	//layouts.fontSize('sm');
	// TODO
	/*setlayout.fontSize = function(layout) {

		switch ( true ) {

			case ( layout === 'sm' ): 
				initApp.pushSettings('mod-skin-light') 
				break;

			case ( layout === 'md' ): 
				initApp.pushSettings('mod-skin-light') 
				break;

			case ( layout === 'lg' ): 
				initApp.pushSettings('mod-skin-light') 
				break;

			case ( layout === 'xl' ): 
				initApp.pushSettings('mod-skin-light') 
				break;

			default: 
				console.log("('" + layout + "')" +" is not a valid entry, enter 'sm', 'md', 'lg', or 'xl'");
				break;
		}	

	};*/

	//layouts.theme(themename.css,true)
	setlayout.theme = function(themename,save) {
		initApp.updateTheme(themename, save);
	};

	//layouts.mode('default');
	setlayout.mode = function(layout) {

		switch ( true ) {

			case ( layout === 'default' ): 
				initApp.removeSettings('mod-skin-light', false);
				initApp.removeSettings('mod-skin-dark', true);
				break;

			case ( layout === 'light' ): 
				initApp.removeSettings('mod-skin-dark', false);
				initApp.pushSettings('mod-skin-light', true);
				break;

			case ( layout === 'dark' ): 
				initApp.removeSettings('mod-skin-light', false);
				initApp.pushSettings('mod-skin-dark', true);
				break;

			default: 
				console.log("('" + layout + "')" +" is not a valid entry, enter 'default', 'light', or 'dark'");
				break;
		}	

	};

	return setlayout;
	
})({});