// require('./lib');
import 'bootstrap';

require('../css/site.scss')

window.$ = window.jQuery = global.jQuery = require("jquery"), require("jquery-ui");

global.test01 = function() {
  console.log("test01");
}

require('jquery-validation');
//require('jquery-validation-unobtrusive');

require('jquery-ui/themes/base/core.css');
require('jquery-ui/themes/base/menu.css');
require('jquery-ui/themes/base/autocomplete.css');
require('jquery-ui/themes/base/theme.css');

require('jquery-ui/ui/widgets/menu');
require('jquery-ui/ui/keycode');
require('jquery-ui/ui/position');
require('jquery-ui/ui/unique-id');
require('jquery-ui/ui/safe-active-element');
require('jquery-ui/ui/version');
require('jquery-ui/ui/widget');

require('jquery-ui/ui/widgets/autocomplete');

require('timeago');

jQuery(document).ready(function() {
  $("time.timeago").timeago();
});

function split( val ) {
  return val.split( /,\s*/ );
}

function extractLast( term ) {
  return split( term ).pop();
}

$( ".autocomplete-tags" )
  // don't navigate away from the field on tab when selecting an item
  .on( "keydown", function( event ) {
    if ( event.keyCode === $.ui.keyCode.TAB &&
        $( this ).autocomplete( "instance" ).menu.active ) {
      event.preventDefault();
    }
  })
  .autocomplete({
    minLength: 0,
    source: function( request, response ) {
      $.getJSON( "/Tag/GetTags", {
                  term: extractLast( request.term )
                }, response );
    },
    focus: function() {
      // prevent value inserted on focus
      return false;
    },
    select: function( event, ui ) {
      var terms = split( this.value );
      // remove the current input
      terms.pop();
      // add the selected item
      terms.push( ui.item.value );
      // add placeholder to get the comma-and-space at the end
      terms.push( "" );
      this.value = terms.join( ", " );
      return false;
    }
  });

$( ".autocomplete-source" )
  // don't navigate away from the field on tab when selecting an item
  .on( "keydown", function( event ) {
    if ( event.keyCode === $.ui.keyCode.TAB &&
        $( this ).autocomplete( "instance" ).menu.active ) {
      event.preventDefault();
    }
  })
  .autocomplete({
    minLength: 0,
    source: function( request, response ) {
      $.getJSON( "/Source/GetSources", {
                  term: extractLast( request.term )
                }, response );
    },
    focus: function() {
      // prevent value inserted on focus
      return false;
    },
    select: function( event, ui ) {
      this.value = ui.item.value;
      return false;
    }
  });

  /*
$('.thumbnail').mouseenter(function (event) {
  $(this).css({"height": "auto"});
});
$('.thumbnail').mouseleave(function (event) {
  $(this).css({"height": "100px"});
});
*/

const feather = require('feather-icons')
feather.replace()

require('bootstrap-datepicker');
$('.datepicker').datepicker({
  format: 'yyyy-mm-dd',
  autoclose: true
});


import Chart from 'chart.js';

require('ammap3');
require('ammap3/ammap/maps/js/worldLow');
require('ammap3/ammap/themes/light');
require('amcharts3');
require('amcharts3/amcharts/pie');
require('amcharts3/amcharts/serial');
require('amcharts3/amcharts/themes/light');

global.monthStatGraph = function(canvasId, data) {
  AmCharts.makeChart( canvasId, {
    "type": "serial",
    "theme": "light",
    "dataProvider": data,
    "valueAxes": [ {
      "gridColor": "#FFFFFF",
      "gridAlpha": 0.2,
      "dashLength": 0
    } ],
    "gridAboveGraphs": true,
    "startDuration": 1,
    "graphs": [ {
      "balloonText": "[[category]]: <b>[[value]]</b>",
      "fillAlphas": 0.8,
      "lineAlpha": 0.2,
      "type": "column",
      "valueField": "count"
    } ],
    "chartCursor": {
      "categoryBalloonEnabled": false,
      "cursorAlpha": 0,
      "zoomable": false
    },
    "categoryField": "label",
    "categoryAxis": {
      "gridPosition": "start",
      "gridAlpha": 0,
      "tickPosition": "start",
      "tickLength": 20
    },
    "fontFamily": "sans"
  } );
};

global.popularTagGraph = function(canvasId, data) {
  console.log(data);
  AmCharts.makeChart( canvasId, {
    "type": "pie",
    "theme": "light",
    "dataProvider": data,
    "valueField": "count",
    "titleField": "tag",
     "balloon":{
     "fixedPosition":true
    },
    "colorField": "tagColor",
    "groupPercent": .05,
    "labelsEnabled": false,
    "legend":{
       "position":"bottom",
    },
    "export": {
      "enabled": true
    },
    "fontFamily": "sans",
  } );
};

global.popularCountryMap = function(canvasId, areas) {
  AmCharts.makeChart( canvasId, {
    "type": "map",
    "theme": "light",
    "colorSteps": 10,
    "zoomOnDoubleClick": false,
    "dragMap": false,
    "autoZoom": false,
    "zoomControl": {
      homeButtonEnabled: false,
      zoomControlEnabled: false,
      panControlEnabled: false
    },
    
    "dataProvider": {
      "map": "worldLow",
      "areas": areas
    },
  
    "export": {
      "enabled": true
    },
    "fontFamily": "sans"
  } );
};

/*
var { Textcomplete, Textarea } = require('textcomplete');
var editor = new Textarea(document.getElementById('textcomplete'));

var textcomplete = new Textcomplete(editor);

textcomplete.register([{ // mention strategy
  match: /(^|\s)@(\w+)$/,
  search: function (term, callback) {
    $.getJSON('/User/Search', { q: term })
      .done(function (resp) { callback(resp); })
      .fail(function ()     { callback([]);   });
  },
  template: function(hit) {
    return hit.displayName;
  },
  replace: function (hit) {
    return '$1@' + hit.userName + ' ';
  },
},{ // doc strategy
  match: /(^|\s)(DI-L[0-9]+)$/,
  search: function (term, callback) {
    $.getJSON('/Document/SearchByReference', { q: term })
      .done(function (resp) { callback(resp); })
      .fail(function ()     { callback([]);   });
  },
  template: function(hit) {
    return hit.displayName;
  },
  replace: function (hit) {
    return '$1' + hit.reference + ' ';
  },
}]);
*/

function titleCase(str) {
    var splitStr = str.toLowerCase().split(' ');
    for (var i = 0; i < splitStr.length; i++) {
        splitStr[i] = splitStr[i].charAt(0).toUpperCase() + splitStr[i].substring(1);     
    }
    return splitStr.join(' '); 
 }

$(document).ready(function(){
    console.log("yada");
    $('.normalize-title').click(function() {
        console.log(titleCase($($(this).data('target'))));
    });

    console.log("yadaaa");
    $('.input-validation-error').parents('.form-group').addClass('has-error');
    $('.input-validation-error').addClass('is-invalid');
    $('.field-validation-error').addClass('text-danger');

    jQuery.each($('a.text-truncate[data-toggle="tooltip"]'), function(index, e) {
      if (e.offsetWidth + 2 < e.scrollWidth) {
        $(e).tooltip();
      } else {
        $(e).prop('title','');
      }
    });

    $(".badge-tooltip").tooltip();
    console.log($(".badge-tooltip"));
});

