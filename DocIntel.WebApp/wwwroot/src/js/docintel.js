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
    form.find('.auto-submit').change(function(){
        $('input[name="page"]').val(1);
        form.submit();
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
    
    $(".autocomplete-tag").each(function(index) {
        $(this).select2({
            placeholder: 'Search for a tag',
            tokenSeparators: [','],
            ajax:
                {
                    url: "/API/Tag/Suggest",
                    dataType: 'json',
                    delay: 250,
                    data: function(params)
                    {
                        return {
                            searchTerm: params.term
                        };
                    },
                    processResults: function (tags) {
                        var data = $.map(tags, function (tag) {
                            tag.id = tag.friendlyName;
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
                "<div>" + "<span class='badge badge-pill " + tag.backgroundColor + "'>" + 
                (tag.facet && tag.facet.prefix ? tag.facet.prefix + ":" : "") + tag.label + "</span>" +
                (tag.keywords && tag.keywords.length > 0 ? "<span class='text-muted ml-2 fs-sm keywords'>(" + tag.keywords.join(', ') + ")</span>" : "")
                + "</div>" +
            '</div>';
        }
    }

    function formatTagSelection (tag) {
        if (tag.hasOwnProperty("id") && tag.id == "")
            return tag.text;

        var prefix = (tag.facet && tag.facet.prefix ? tag.facet.prefix: "");
        var label = (tag.label || tag.text).trim();

        var backgroundColor = tag.backgroundColor;
        if (!backgroundColor && tag.hasOwnProperty("element") && tag.element.dataset.hasOwnProperty("backgroundcolor")) {
            backgroundColor = tag.element.dataset.backgroundcolor;
        }
        backgroundColor = (backgroundColor || "bg-success-50").trim();

        return "<span class='badge badge-pill " + (backgroundColor) + "'>"
            + (prefix ? prefix + ":" : "") + (label) + "</span></div>";
    }
}

export function autocompleteFacet () {
    console.log("🦛 Installing autocomplete for facets ...");

    // Autocomplete Facets
    
    $(".autocomplete-facet").each(function (index) {
        $(this).select2({
            placeholder: 'Search for a facet',
            ajax: {
                url: "/API/Facet/Suggest",
                dataType: 'json',
                delay: 250,
                data: function (params) {
                    return {
                        searchTerm: params.term
                    };
                },
                processResults: function (tags) {
                    return {
                        results: tags
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
                    url: "/API/Source/Suggest",
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