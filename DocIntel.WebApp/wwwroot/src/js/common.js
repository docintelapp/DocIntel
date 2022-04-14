import "bootstrap"

// import '../smartadmin/webfonts/fontawesome-pro-master/scss/fontawesome'
// import '@fortawesome/fontawesome-free/js/regular'
// import '@fortawesome/fontawesome-free/js/brands'
// import '@fortawesome/fontawesome-free/js/solid'

import "../smartadmin/js/_get/app.get.colors"
import "../smartadmin/js/_config/app.config"

import "../smartadmin/js/_modules/app.navigation"
import "../smartadmin/js/_modules/app.init"
import "../smartadmin/js/_modules/app.window.load"
import "../smartadmin/js/_modules/app.domReady"

import "../smartadmin/js/_modules/app.menu.slider"
// import "../smartadmin/js/_modules/app.resize.trigger"
// import "../smartadmin/js/_modules/app.scroll.trigger"
// import "../smartadmin/js/_modules/app.orientationchange"

import "../smartadmin/custom/plugins/jquery-sparkline/jquery-sparkline.config"
import "../smartadmin/custom/plugins/jquery-ui-cust/jquery-ui-cust.js";

import 'timeago';
import 'bootstrap-sass-datepicker';
import 'select2';
import 'node-waves';

import * as d3 from 'd3';
import Datamap from 'datamaps';
import "../datamaps.world.hires.min.js";

import * as britecharts from 'britecharts';
import 'ion-rangeslider';

var { Textcomplete, Textarea } = require('textcomplete');
    

import CodeMirror from 'codemirror/lib/codemirror';
import 'codemirror/mode/xml/xml';

import 'summernote/dist/summernote-bs4';

import clamp from 'clamp-js';


import { DataSet } from 'vis-data';
import { Network } from 'vis-network';

var titleCase = function (str) {
    var splitStr = str.split(' ');
    for (var i = 0; i < splitStr.length; i++) {
        if (splitStr[i].length > 3) {
            splitStr[i] = splitStr[i].toLowerCase();
            splitStr[i] = splitStr[i].charAt(0).toUpperCase() + splitStr[i].substring(1);
        }
    }
    return splitStr.join(' '); 
 }


// $(document).load(function() {
//     console.log("Load");
// })

import 'qrcode';

$(document).ready(function(){
    
    function resizeIFrameToFitContent(iFrame) {
        console.log(iFrame.offsetWidth);
        iFrame.height = Math.min(iFrame.offsetWidth * 1.414 + 50, 1235);
    }

    var iFrame = document.getElementsByTagName('iframe');
    for (var i = 0; i < iFrame.length; i++) {
        resizeIFrameToFitContent(iFrame[i]);
    }

    jQuery('a[data-toggle="tab"]').on('shown.bs.tab', function (e) {
        var iFrame = document.getElementsByTagName('iframe');
        for (var i = 0; i < iFrame.length; i++) {
            resizeIFrameToFitContent(iFrame[i]);
        }
    })

    console.log(clamp);
    var module = document.getElementsByClassName("clamp-text");
    Array.from(module).forEach(e => clamp(e, { clamp: 5 }));

    if (document.getElementById('mynetwork') != null) {

        var convertDataToNode = function (responseData, nodeData) {
            nodeData.id = responseData.entityId;
            nodeData.label = responseData.name;
            if (responseData.entityType == "malware") {
                nodeData.shape = "circularImage";
                nodeData.image = "/images/icons/color/icons8-virus-48.png";
                nodeData.imagePadding = 5;
                nodeData.size = 24;

            } else if (responseData.entityType == "threatActor") {
                nodeData.shape = "circularImage";
                nodeData.image = "/images/icons/color/icons8-spy-48.png";
                nodeData.imagePadding = 5;
                nodeData.size = 24;

            } else if (responseData.entityType == "attackPattern") {
                nodeData.shape = "circularImage";
                nodeData.image = "/images/icons/color/icons8-workflow-48.png";
                nodeData.imagePadding = 5;
                nodeData.size = 24;
            }
            nodeData.color = {
                border: "#d9d9d9",
                background: "#efefef",
            }
            return nodeData;
        };

        var documentId = $('#mynetwork').data('document-id');
        console.log(documentId);

        // create a network
        var container = document.getElementById('mynetwork');

        var dataStoreNode = {};
        var nodes = [];
        var edges = [];

        jQuery.get(
            "/CTI/Report/GetEntities", 
            {
                documentId: documentId
            },
            function (responseData, textStatus, jqXHR) {
                console.log(responseData);
                $.each( responseData.entities, function( key, value ) {
                    dataStoreNode[value.entityId] = value;
                    var n = convertDataToNode(value, new Object());
                    console.log(n);
                    nodes.push(n);
                });

                // provide the data in the vis format
                var data = {
                    nodes: nodes,
                    edges: edges
                };

                var options = {
                    interaction: {
                    navigationButtons: true,
                    keyboard: true
                    },
                    manipulation: {
                        enabled: true,
                        addNode: function (data, callback) {
                            console.log(data);
                            $('#exampleModal').modal();
                            $('#modal-save').unbind('click');
                            $('#modal-save').on('click', function() {
                                $('#exampleModal').modal('hide');
                                jQuery.post(
                                    "/CTI/Report/RelatesTo", 
                                    {
                                        entityType: $('#modal-entity-type').val(),
                                        entityId: $('#modal-entity-id').val(),
                                        documentId: $('#modal-document-id').val(),
                                    },
                                    function (responseData, textStatus, jqXHR) {
                                        dataStoreNode[responseData.entityId] = responseData.entity;
                                        data = convertDataToNode(responseData.entity, data);
                                        callback(data);
                                    },
                                    "json"
                                );
                            });
                        },
                        addEdge: function (edgeData, callback) {
                            console.log(edgeData);
                            edgeData.font = {
                                size: 12
                            };

                            var fromNode = dataStoreNode[edgeData.from];
                            var toNode = dataStoreNode[edgeData.to];
                            if (fromNode.entityType == "threatActor") {
                                if (toNode.entityType == "malware") {
                                    edgeData.arrow = {'to': { 'enabled': true }};
                                    edgeData.label = "uses";
                                    callback(edgeData);
                                } else if (toNode.entityType == "attackPattern") {
                                    edgeData.arrow = {'to': { 'enabled': true }};
                                    edgeData.label = "uses";
                                    callback(edgeData);
                                }
                                // No other outgoing connection

                            } else if (fromNode.entityType == "malware") {
                                if (toNode.entityType == "attackPattern") {
                                    edgeData.arrow = {'to': { 'enabled': true }};
                                    edgeData.label = "uses";
                                    jQuery.post(
                                        "/CTI/Report/AddRelation", 
                                        {
                                            entityTypeFrom: fromNode.entityType,
                                            entityIdFrom: fromNode.entityId,
                                            entityTypeTo: toNode.entityType,
                                            entityIdTo: toNode.entityId,
                                            relationType: "uses",
                                            documentId: documentId
                                        },
                                        function (responseData, textStatus, jqXHR) {
                                            callback(edgeData);
                                        },
                                        "json"
                                    );
                                }
                                // No other outgoing connection

                            } else if (fromNode.entityType == "attackPattern") {
                                // No outgoing connections so far
                            }
                        }
                    }
                };
                
                console.log(data);
                var network = new Network(container, data, options);


            },
            "json"
        );

        
    }

    $(".js-range-slider").ionRangeSlider();
    $(".source-range-slider").ionRangeSlider({
        "prettify": function(n) {
            if (n >= 0)
                return n;
            else 
                return "Unknown";
        }
    });
    
    $(".source-search-range-slider").ionRangeSlider({
        "onFinish": function (data) {
            $("form").submit();
        },
        "prettify": function(n) {
            if (n >= 0)
                return n;
            else 
                return "Unknown";
        }
    });
    
    $('.normalize-title').click(function() {
        var element = $('#' + $(this).data('target'));
        var value = element.val();
        element.val(titleCase(value));
    });

    $('.input-validation-error').parents('.form-group').addClass('has-error');
    $('.input-validation-error').addClass('is-invalid');
    $('.field-validation-error').addClass('text-danger invalid-feedback');

    console.log("Installing textcomplete");

    if (document.getElementById('textcomplete') != null) {
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
        match: /(^|\s)(DI-[A-Z0-9]+)$/,
        search: function (term, callback) {
            $.getJSON('/Document/SearchByReference', { prefix: term })
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
    }

    const colorPalette = [
        myapp_get_color.primary_500,
        myapp_get_color.success_500,
        myapp_get_color.info_500,
        myapp_get_color.warning_500,
        myapp_get_color.danger_500
    ];

    $(".map-container").each(function(index) {
        console.log(this);
        var map = new Datamap({
            element: this,
            projection: 'mercator',
            fills: {
              defaultFill: '#ededed',
              origin: myapp_get_color.primary_500
            },
            data: {
              USA: { fillKey: "origin" }
            }
          });
    });

    console.log("Installing summernote");
    $(".summernote").summernote({
        height: 300,   //set editable area's height
        codemirror: { // codemirror options
          CodeMirrorConstructor: CodeMirror,
          theme: 'monokai'
        },
        toolbar: [
        ['block', ['style']],
        ['style', ['bold', 'italic', 'underline', 'clear']],
        ['font', ['strikethrough', 'superscript', 'subscript']],
        ['color', ['color']],
        ['para', ['ul', 'ol', 'paragraph']],
        ['insert', ['picture', 'link', 'table']],
        ['undo', ['undo', 'redo']],
    ]
    });

    // console.log("Testing britechart");

    $(".line-container").each(function(index) {
        const lineContainer = d3.select(this);
        const lineContainerWidth = lineContainer.node().getBoundingClientRect().width ;

        const lineChart = britecharts.line();
        console.log($(this).data("url"));

        lineChart
            .isAnimated(true)
            .colorSchema(colorPalette)
            .height(120)
            .margin({top: 10, bottom: 30, left: 30, right: 0})
            .yTicks(1)
            .xAxisFormat('custom')
            .xAxisCustomFormat('%e %b')
            .lineCurve('monotoneX')
            .width(lineContainerWidth);
            
        $.ajax({
            url: $(this).data("url")
        }).done(function (data) {
            lineContainer.datum(data).call(lineChart);
        });

    });

    // const lineContainer = d3.select('.line-container');
    // const lineContainerWidth = lineContainer.node().getBoundingClientRect().width ;
    // const lineChart = britecharts.line();
    // lineChart
    //     .colorSchema(colorPalette)
    //     .height(200)
    //     .margin({top: 0, bottom: 30, left: 30, right: 0})
    //     .lineCurve('basis')
    //     .grid('vertical')
    //     .width(lineContainerWidth);
    // lineContainer.datum(dataSetLineChart).call(lineChart);                    
    
    // // Test BriteChart
    // const container = d3.select('.bar-container');
    // const legendContainer = d3.select('.legend-container');
    // const donutChart = britecharts.donut();
    // const legendChart = britecharts.legend();
    // const dataset = [
    //     {
    //         quantity: 25,
    //         percentage: 25,
    //         name: 'activist',
    //         id: 1
    //     },
    //     {
    //         quantity: 25,
    //         percentage: 25,
    //         name: 'hacker',
    //         id: 2
    //     },
    //     {
    //         quantity: 50,
    //         percentage: 50,
    //         name: 'nation-state',
    //         id: 3
    //     }
    // ];
    // const containerWidth = jQuery('.bar-container')[0].clientWidth;
    // donutChart
    //     .colorSchema(colorPalette)
    //     .isAnimated(true)
    //     .highlightSliceById(3)
    //     .width(containerWidth)
    //     .height(containerWidth)
    //     .externalRadius(containerWidth/2.5)
    //     .internalRadius(containerWidth/5);

    // legendChart
    //     .isHorizontal(true)
    //     .markerSize(8)
    //     .height(40)
    //     .width(containerWidth*0.6);

    // container.datum(dataset).call(donutChart);
    // legendContainer.datum(dataset).call(legendChart);

    $("time.timeago").timeago();

    $('.datepicker-control').datepicker({
        format: 'yyyy-mm-dd',
        autoclose: true
    });

    $(".autocomplete-tags").each(function(index) {
        console.log($(this));
        $(this).select2({
            placeholder: 'Search for a tag',
            tokenSeparators: [','],
            ajax:
            {
                url: "/Tag/GetTags",
                dataType: 'json',
                delay: 250,
                data: function(params)
                {
                    return {
                        term: params.term
                    };
                },
                processResults: function (tags) {
                    var data = $.map(tags, function (obj) {
                        obj.children = $.map(obj.children, function (tag) {
                            console.log(tag);
                            if (tag.prefix)
                                tag.id = tag.prefix + ":" + tag.label;
                            else
                                tag.id = tag.label;
                            // tag.id = tag.tagId;
                            return tag;
                        });
                        return obj;
                    });
                    return {
                        results: data
                    };
                }
            },
            createTag: function (params) {
                var term = $.trim(params.term);
                
                if (term === '') {
                    return null;
                }

                var prefix = term.substring(0, term.indexOf(":") );
                var label = term.substring(term.indexOf(":") + 1);

                return {
                    id: term,
                    text: term,
                    newTag: true,
                    label: label,
                    prefix: prefix
                }
            },
            cache: true,
            escapeMarkup: function(markup)
            {
                return markup;
            }, // let our custom formatter work
            minimumInputLength: 1,
            templateResult: formatTag,
            templateSelection: formatTagSelection
        });
    });

        

    function formatTag(tag) {
        if (tag.loading)
        {
            return tag.text;
        }

        if (tag.title) 
            return tag.title;

        if (tag.newTag) {
            return "<div class='clearfix d-flex'>" +
                "<div>" + "Create new tag: <span class='badge badge-pill bg-success-50'>" + tag.text + "</span></div>" +
            '</div>';
        }
            
        if (tag.label) {
            return "<div class='clearfix d-flex'>" +
                "<div>" + "<span class='badge badge-pill " + tag.backgroundColor + "'>" + tag.label + "</span></div>" +
                "<div>" + 
                    (tag.description ? "<div class='opacity-70 ml-2 mr-2'>" + tag.description + "</div>" : "") +
                    (tag.keywords ? "<div class='opacity-70 ml-2'><b>Keywords:</b> " + tag.keywords + "</div>" : "") + 
                "</div>" +
            '</div>';
        }
    }

    function formatTagSelection (tag) {
        console.log(tag);
        if (tag.hasOwnProperty("id") && tag.id == "")
             return tag.text;
        
        var prefix = "";
        if (tag.hasOwnProperty("prefix")) {
            prefix = tag.prefix;
        } else if(tag.hasOwnProperty("element") && tag.element.dataset.hasOwnProperty("prefix")) {
            prefix = tag.element.dataset.prefix;
        }
        prefix = prefix.trim()
        
        var label = (tag.label || tag.text).trim();
        
        var backgroundColor = tag.backgroundColor; 
        if (!backgroundColor && tag.hasOwnProperty("element") && tag.element.dataset.hasOwnProperty("backgroundcolor")) {
            backgroundColor = tag.element.dataset.backgroundcolor;
        }
        backgroundColor = (backgroundColor || "bg-success-50").trim();

        return "<span class='badge badge-pill " + (backgroundColor) + "'>" 
            + (prefix ? prefix + ":" : "") + (label) + "</span></div>";
    }

    $(".autocomplete-color").select2(
        {
            placeholder: 'Search for a color',
            createTag: function (params) {
                var term = $.trim(params.term);
                
                if (term === '') {
                    return null;
                }

                return {
                    id: term,
                    text: term,
                    class: term
                }
            },
            cache: true,
            escapeMarkup: function(markup)
            {
                return markup;
            },
            minimumInputLength: 1,
            templateResult: formatColor,
            templateSelection: formatColorSelection
        });

    function formatColor(color) {
        if (color.loading)
        {
            return color.text;
        }
        return "<span class='badge badge-pill " + color.id + "'>" + color.text + "</span>";
    }

    function formatColorSelection (color) {
        return "<span class='badge badge-pill " + color.id + "'>" + color.text + "</span>";
    }

    $(".autocomplete-source").select2(
        {
            placeholder: 'Search for a source',
            ajax:
            {
                url: "/API/Source",
                dataType: 'json',
                delay: 250,
                data: function(params)
                {
                    return {
                        searchTerm: params.term + "*",
                        sortCriteria: 1
                    };
                },
                processResults: function (sources) {
                    var data = $.map(sources, function (source) {
                        source.id = source.id || source.sourceId;
                        return source;
                    });
                    return {
                        results: data
                    };
                }
            },
            createTag: function (params) {
                var term = $.trim(params.term);
                
                if (term === '') {
                    return null;
                }

                return {
                    id: term,
                    text: term,
                    newSource: true
                }
            },
            cache: true,
            minimumInputLength: 1,
            escapeMarkup: function(markup)
            {
                return markup;
            },
            templateResult: function(source)
            {
                console.log(source);

                if (source.loading)
                {
                    return source.text;
                }

                if (source.newSource) {
                    return "<div class='clearfix d-flex'>" +
                        "<div>" + "Create new source: <span class='badge badge-pill bg-success-50'>" + source.text + "</span></div>" +
                    '</div>';
                }

                if (source.title) 
                    return source.title;
            },
            templateSelection: function(source)
            {
                return source.title || source.text;
            }
        });


});


window.initSearchPage = function(form_id) {
    var form = $(form_id);
    jQuery(document).ready(function() {
        form.find('.auto-submit').change(function(){
            form.submit();
        });
        form.find('.page-link-autosubmit').click(function() {
            $('input[name="page"]').val($(this).data("page"));
            console.log($(this).data("page"));
            form.submit();
        });
    });
}