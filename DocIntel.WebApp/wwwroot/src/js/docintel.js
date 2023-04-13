import '../../dist2/js/formplugins/select2/select2.bundle.js';
import '../../dist2/js/formplugins/ion-rangeslider/ion-rangeslider.js';
import '../../dist2/js/formplugins/summernote/summernote.js';
import '../../dist2/js/formplugins/dropzone/dropzone.js';
import '../../dist2/js/formplugins/bootstrap-datepicker/bootstrap-datepicker.js';

import clamp from "clamp-js";
import 'timeago';
import hljs from 'highlight.js';

/* Resize iFrames */
export function resizeIFrameToFitContent(iFrame) {
    iFrame.height = Math.min(iFrame.parentElement.offsetWidth * 1.414 + 50, 1235);
}

export function initApp() {
    $(".select2").select2();
    $(document).on('select2:open', () => {
        document.querySelector('.select2-container--open .select2-search__field').focus();
    });
    
    autocompleteFacet();
    autocompleteTags();
    autocompleteColors();
    autocompleteSource();

    initSliders();
    
    initErrorMessages();
    initNormalizeTitle();

    document.querySelectorAll('pre code').forEach((block) => {
        hljs.highlightBlock(block);
    });
    
    var iFrame = document.getElementsByTagName('iframe');
    for (var i = 0; i < iFrame.length; i++) {
        console.log("🦛 Resize iframes...")
        resizeIFrameToFitContent(iFrame[i]);
    }    
    
    /*
    $('[data-toggle="tab"]').on('shown.bs.tab', function (e) {
        console.log("🦛 Resize iframes...")
        var iFrame = document.getElementsByTagName('iframe');
        for (var i = 0; i < iFrame.length; i++) {
            resizeIFrameToFitContent(iFrame[i]);
        }
    })
    */
    
    /* Clamp text */
    var module = document.getElementsByClassName("clamp-text");
    Array.from(module).forEach(e => clamp(e, { clamp: 5 }));

    /* Timeage */
    $("time.timeago").timeago();

    /* Datepicker */
    $('.datepicker-control').datepicker({
        format: 'yyyy-mm-dd',
        autoclose: true,
        todayBtn: true,
        clearBtn: true
    });

}

export function initSearchPage(form_id) {
    var form = $(form_id);
    var noFilterText = $('#search-filters').html();
    var filters = {}
    
    $('#clear-all-filters').click(function() {
       filters = {}
       form.submit();
    });

    $('#save-default-search').click(function() {
        console.log("TODO");
        form.attr('action', $(this).data('form-action'));
        console.log(form.action);
        form.submit();
    });
    
    var dropDownMenu = function(filterId) {
        return $('<div class="dropdown float-left mr-1" />')
            .append($('<a href="#" class="px-1" data-toggle="dropdown"  />').html('&#8942;'))
            .append($('<div class="dropdown-menu" />')
                .append(
                    $('<a class="dropdown-item py-1 px-3" href="javascript:void(0)">Delete</a>')
                        .click(function() {
                            delete filters[filterId];
                            form.submit();
                        })
                )
                .append($('<a class="dropdown-item py-1 px-3" href="javascript:void(0)">Invert</a>')
                    .click(function() {
                        filters[filterId].negate = !filters[filterId].negate;
                        form.submit();
                    }))
            );
    }
    
    var removeValue = function(filter, id) {
        if (filters[filter] !== undefined) {
            delete filters[filter]['values'][id];
            if (Object.keys(filters[filter]['values']).length === 0) {
                delete filters[filter];
            }
        }
        form.submit();
    }
    
    var displayFilters = function() {
        var searchFilters = $('#search-filters');
        searchFilters.empty();
        if (Object.keys(filters).length === 0) {
            searchFilters.append(noFilterText);
        } else {
            $.each(filters, function (index, filter) {
                searchFilters.append(
                    $('<div class="d-inline-block border py-1 px-2 mr-2 mb-2" />')
                        .append(dropDownMenu(filter.id))
                        .append($('<span />').text(filter.name))
                        .append(filter.negate ? $('<span class="text-danger ml-2" />').text("NOT") : '')
                        .append(Object.entries(filter.values).map(([k,val]) => 
                            $('<a class="badge badge-pill badge-secondary ml-2" />')
                                .addClass(val.color ? val.color : '')
                                .text(val.name)
                                .click(function() {
                                    removeValue(filter.id, k);
                                })
                        ))
                );
            });
        }
    }

    var selectedFilters = $("#selected-filters").val();
    if (selectedFilters) {
        var sf = JSON.parse(atob(selectedFilters));
        $.each(sf, function (index, filter) {
            console.log(filter);
            filters[filter.Id] = {
                'id': filter.Id,
                'name': filter.Name,
                'negate': filter.Negate,
                'field': filter.Field,
                'operator': filter.Operator,
                'values': filter.Values.map((s) => {
                    return {'id': s.Id, 'name': s.Name, 'color': s.Color};
                })
            }
        });
        displayFilters();
    }

    $('.tag-search-selection .fa-search-plus').click(function(eventObject) {
        var defaultFilter = $(this).parents('.tag-search-selection').data('default-filter');
        var defaultFilterName = $(this).parents('.tag-search-selection').data('default-filter-name');
        var field = $(this).parents('.tag-search-selection').data('default-filter-field');
        if (filters[defaultFilter] === undefined) {
            filters[defaultFilter] = {
                'id': defaultFilter,
                'name': defaultFilterName,
                'negate': false,
                'field': field,
                'operator': 'oneof',
                'values': {}
            }
        }
        
        let id = "#" + $(this).data('select-id');
        let selectedData = $(id).select2('data')[0];
        console.log(selectedData);
        
        var valueId = selectedData.id;
        var valueName = selectedData.label;
        if (valueId !== undefined && valueName !== undefined) {
            filters[defaultFilter]['values'][valueId] = { 'id': valueId, 'name': valueName };

            var valueColor = selectedData.backgroundColor;
            if (valueColor !== undefined) {
                filters[defaultFilter]['values'][valueId].color = valueColor;
            }
        }

        form.submit();
    });
    
    $('.tag-search-selection .fa-search-minus').click(function(eventObject) {
        var defaultFilter = '!' + $(this).parents('.tag-search-selection').data('default-filter');
        var defaultFilterName = $(this).parents('.tag-search-selection').data('default-filter-name');
        var field = $(this).parents('.tag-search-selection').data('default-filter-field');
        if (filters[defaultFilter] === undefined) {
            filters[defaultFilter] = {
                'id': defaultFilter,
                'name': defaultFilterName,
                'negate': true,
                'field': field,
                'operator': 'oneof',
                'values': {}
            }
        }

        let id = "#" + $(this).data('select-id');
        let selectedData = $(id).select2('data')[0];

        var valueId = selectedData.id;
        var valueName = selectedData.label;
        if (valueId !== undefined && valueName !== undefined) {
            filters[defaultFilter]['values'][valueId] = { 'id': valueId, 'name': valueName };

            var valueColor = selectedData.backgroundColor;
            if (valueColor !== undefined) {
                filters[defaultFilter]['values'][valueId].color = valueColor;
            }
        }

        form.submit();
    });


    $('.tag-selection .fa-search-plus').click(function(eventObject) {
        var defaultFilter = $(this).parents('.tag-selection').data('default-filter');
        var defaultFilterName = $(this).parents('.tag-selection').data('default-filter-name');
        var field = $(this).parents('.tag-selection').data('default-filter-field');
        if (filters[defaultFilter] === undefined) {
            filters[defaultFilter] = {
                'id': defaultFilter,
                'name': defaultFilterName,
                'negate': false,
                'field': field,
                'operator': 'oneof',
                'values': {}
            }
        }

        var valueId = $(this).parents('.tag-selection').data('value-id');
        var valueName = $(this).parents('.tag-selection').data('value-name');
        if (valueId !== undefined && valueName !== undefined) {
            filters[defaultFilter]['values'][valueId] = { 'id': valueId, 'name': valueName };

            var valueColor = $(this).parents('.tag-selection').data('value-color');
            if (valueColor !== undefined) {
                filters[defaultFilter]['values'][valueId].color = valueColor;
            }
        }

        form.submit();
    });
    
    $('.tag-selection .fa-search-minus').click(function(eventObject) {
        var defaultFilter = '!' + $(this).parents('.tag-selection').data('default-filter');
        var defaultFilterName = $(this).parents('.tag-selection').data('default-filter-name');
        var field = $(this).parents('.tag-selection').data('default-filter-field');
        if (filters[defaultFilter] === undefined) {
            filters[defaultFilter] = {
                'id': defaultFilter,
                'name': defaultFilterName,
                'negate': true,
                'field': field,
                'operator': 'oneof',
                'values': {}
            }
        }

        var valueId = $(this).parents('.tag-selection').data('value-id');
        var valueName = $(this).parents('.tag-selection').data('value-name');
        if (valueId !== undefined && valueName !== undefined) {
            filters[defaultFilter]['values'][valueId] = { 'id': valueId, 'name': valueName };

            var valueColor = $(this).parents('.tag-selection').data('value-color');
            if (valueColor !== undefined) {
                filters[defaultFilter]['values'][valueId].color = valueColor;
            }
        }
        
        form.submit();
    });
    
    form.submit(function() {
        console.log("Clear all filters");
        $(this).children('input.filter-field').remove();
        
        console.log("Add all filters");
        console.log(filters);
        $.each(Object.values(filters), function (index, filter) {
            form.append($('<input type="hidden" class="filter-field" />')
                .attr('name', 'filters[' + index + '].id')
                .val(filter.id));
            form.append($('<input type="hidden" class="filter-field" />')
                .attr('name', 'filters[' + index + '].name')
                .val(filter.name));
            form.append($('<input type="hidden" class="filter-field" />')
                .attr('name', 'filters[' + index + '].negate')
                .val(filter.negate));
            form.append($('<input type="hidden" class="filter-field" />')
                .attr('name', 'filters[' + index + '].field')
                .val(filter.field));
            form.append($('<input type="hidden" class="filter-field" />')
                .attr('name', 'filters[' + index + '].operator')
                .val(filter.operator));
            
            $.each(Object.values(filter.values), function(indexVal, val) {
                form.append($('<input type="hidden" class="filter-field" />')
                    .attr('name', 'filters[' + index + '].values['+indexVal+'].id')
                    .val(val.id))
                form.append($('<input type="hidden" class="filter-field" />')
                    .attr('name', 'filters[' + index + '].values['+indexVal+'].name')
                    .val(val.name))
                if (val.color) {
                    form.append($('<input type="hidden" class="filter-field" />')
                        .attr('name', 'filters[' + index + '].values[' + indexVal + '].color')
                        .val(val.color))
                }
            });
        });
        
        return true;
    });
    
    form.find('.page-link-autosubmit').click(function() {
        $('input[name="page"]').val($(this).data("page"));
        form.submit();
    });
}

export function initDataTable() {
//    console.log("🦛 Installing datatables ...");
//    $(".data-table").each(function() {
//        $(this).DataTable();
//    });
}

export function initNormalizeTitle() {
    var titleCase = function (str) {
        var splitStr = str.split(' ');
        for (var i = 0; i < splitStr.length; i++) {
            splitStr[i] = splitStr[i].toLowerCase();
            if (splitStr[i].length > 3) {
                splitStr[i] = splitStr[i].charAt(0).toUpperCase() + splitStr[i].substring(1);
            }
        }
        return splitStr.join(' ');
    }

    $('.normalize-title').click(function() {
        var element = $('#' + $(this).data('target'));
        var value = element.val();
        element.val(titleCase(value));
    });
}

export function initErrorMessages() {
    $('.input-validation-error').parents('.form-group').addClass('has-error');
    $('.input-validation-error').addClass('is-invalid');
    $('.field-validation-error').addClass('text-danger invalid-feedback');
}

export function initSliders() {
    console.log("🦛 Installing sliders ...");
    
    $(".js-range-slider").ionRangeSlider();
    $(".source-range-slider").ionRangeSlider({
        "prettify": function(n) {
            if (n >= 0)
                return n;
            else
                return "Unknown";
        }
    });
}

export function autocompleteColors() {
    console.log("🦛 Installing autocomplete for colors ...");
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
}

export function autocompleteTags() {
    console.log("🦛 Installing autocomplete for tags ...");
    
    $(".autocomplete-tag").each(function(index, item) {
        let format = $(this).data('format');
        
        $(this).select2({
            placeholder: 'Search for a tag',
            tokenSeparators: [','],
            ajax:
                {
                    url: "/API/Suggest/Tag",
                    dataType: 'json',
                    delay: 250,
                    data: function(params)
                    {
                        return {
                            facetPrefix: item.dataset.facet,
                            searchTerm: params.term
                        };
                    },
                    processResults: function (tags) {
                        var data = $.map(tags, function (tag) {
                            if (item.dataset.idValue === "true") {
                                tag.id = tag.tag_id;
                            } else {
                                tag.id = tag.friendly_name;
                            }
                            return tag;
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
                    facet: {
                        prefix: prefix
                    }
                }
            },
            cache: true,
            escapeMarkup: function(markup)
            {
                return markup;
            }, // let our custom formatter work
            minimumInputLength: 2,
            templateResult: function(t) {
                return formatTag(t, format);
            },
            templateSelection: function(t) {
            return formatTagSelection(t, format);
        },
        });
    });

    function formatTag(tag, format) {
        console.log(format);
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
            if (format === "short") {
                return "<div class='clearfix d-flex'>" +
                    "<div>" + "<span class='badge badge-pill " + tag.background_color + "'>" + tag.label + "</span>" + '</div>';
                
            } else {
                return "<div class='clearfix d-flex'>" +
                    "<div>" + "<span class='badge badge-pill " + tag.background_color + "'>" +
                    (tag.facet && tag.facet.prefix ? tag.facet.prefix + ":" : "") + tag.label + "</span>" +
                    (tag.keywords && tag.keywords.length > 0 ? "<span class='text-muted ml-2 fs-sm keywords'>(" + tag.keywords.join(', ') + ")</span>" : "")
                    + "</div>" +
                    '</div>';
            }
        }
    }

    function formatTagSelection (tag, format) {
        if (tag.hasOwnProperty("id") && tag.id == "")
            return tag.text;

        var prefix = (tag.facet && tag.facet.prefix ? tag.facet.prefix: "");
        var label = (tag.label || tag.text).trim();

        var backgroundColor = tag.background_color;
        if (!backgroundColor && tag.hasOwnProperty("element") && tag.element.dataset.hasOwnProperty("backgroundcolor")) {
            backgroundColor = tag.element.dataset.backgroundcolor;
        }
        backgroundColor = (backgroundColor || "bg-success-50").trim();
        if (format === "short") {
            return "<span class='badge badge-pill " + (backgroundColor) + "'>" + (label) + "</span></div>";
        } else {
            return "<span class='badge badge-pill " + (backgroundColor) + "'>"
                + (prefix ? prefix + ":" : "") + (label) + "</span></div>";   
        }
    }
}

export function autocompleteFacet () {
    console.log("🦛 Installing autocomplete for facets ...");

    // Autocomplete Facets
    
    $(".autocomplete-facet").each(function (index) {
        $(this).select2({
            placeholder: 'Search for a facet',
            ajax: {
                url: "/API/Suggest/Facet/",
                dataType: 'json',
                delay: 250,
                data: function (params) {
                    return {
                        searchTerm: params.term
                    };
                },
                processResults: function (facets) {
                    var data = $.map(facets, function (facet) {
                        facet.id = facet.id || facet.facet_id;
                        return facet;
                    });
                    return {
                        results: data
                    };
                }
            },
            cache: true,
            escapeMarkup: function (markup) {
                return markup;
            }, // let our custom formatter work
            minimumInputLength: 1,
            templateResult: formatTagFacet,
            templateSelection: formatTagFacetSelection
        });
    });


    function formatTagFacet(tag) {
        console.log(tag);
        if (tag.loading) {
            return tag.text;
        }

        if (tag.title) {
            return tag.title;
        }
        
        if (tag.newTag) {
            return "<div class='clearfix d-flex'>" +
                "<div>" + "Create new facet: <span class='badge badge-pill bg-success-50'>" + tag.text + "</span></div>" +
                '</div>';
        }
    }

    function formatTagFacetSelection(tag) {
        return (tag.title ? tag.title : tag.text);
    }
}

export function autocompleteSource () {
    console.log("🦛 Installing autocomplete for sources ...");

    $(".autocomplete-source").select2(
        {
            placeholder: 'Search for a source',
            ajax:
                {
                    url: "/API/Suggest/Source",
                    dataType: 'json',
                    delay: 250,
                    data: function(params)
                    {
                        return {
                            searchTerm: params.term
                        };
                    },
                    processResults: function (sources) {
                        var data = $.map(sources, function (source) {
                            source.id = source.id || source.source_id;
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
            minimumInputLength: 2,
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
}