import "../css/common.scss";

import { initApp, resizeIFrameToFitContent, initSearchPage } from './docintel'
import '../../dist2/js/formplugins/summernote/summernote.js';
import QRCode from "qrcode";

import { JSONEditor } from '@json-editor/json-editor/dist/jsoneditor.js'


$(document).ready(function() {
    console.log("🦛 Installing summernote...");
    $('.summernote').each(function () {
        $(this).summernote({
            height: 200,
            toolbar: [
                ['style', ['style']],
                ['font', ['bold', 'underline', 'clear']],
                ['color', ['color']],
                ['para', ['ul', 'ol', 'paragraph']],
                ['table', ['table']],
                ['insert', ['link', 'picture']],
                ['view', ['fullscreen', 'codeview', 'help']],
            ],
            styleTags: [
                'p',
                { title: 'Heading 1', tag: 'h2', value: 'h2' },
                { title: 'Heading 2', tag: 'h3', value: 'h3' },
                { title: 'Heading 3', tag: 'h4', value: 'h4' },
                'pre'
            ],
        })
    });

    $('a[data-toggle="tab"]').on('shown.bs.tab', function (e) {
        console.log("🦛 Resize iframes...")
        var iFrame = document.getElementsByTagName('iframe');
        for (var i = 0; i < iFrame.length; i++) {
            resizeIFrameToFitContent(iFrame[i]);
        }
    })

    $('.qr-code-canvas').each(function () {
        const text = this.dataset.authenticatorUri;
        QRCode.toCanvas(this, text, function (error) {
            if (error) console.error(error)
            console.log('success!');
        })
    })

    $('.autosubmit-form').each(function (index, node) {
        console.log("🦛 Init autosubmit forms...")
        initSearchPage(node);
    })

    $('.editorJSON').each(function (index, node) {
        console.log("🦛 Init json-editor forms...")

        let schema = JSON.parse(atob($(node).data("schema")));
        let options = {
            theme: 'bootstrap5',
            disable_collapse: true,
            disable_edit_json: true,
            disable_properties: true,
            iconlib: 'fontawesome5',
            schema: schema
        };

        if ($(node).data("startval") && $(node).data("startval").length > 0) {
            options.startval = JSON.parse(atob($(node).data("startval")));
        }

        let editor = new JSONEditor(node, options);

        let inputSelector = '[name="Settings"]';
        if ($(node).data("inputid") && $(node).data("inputid").length > 0) {
            inputSelector = '#' + $(node).data("inputid") + '';
        }

        let dict = '';
        if ($(node).data("dict") && $(node).data("dict").length > 0) {
            dict = $(node).data("dict");
        }
        
        $(this).parents("form").submit(function() {
            $(inputSelector).val(JSON.stringify(editor.getValue()));
            return true;
        })
    })
    
    initApp();
})