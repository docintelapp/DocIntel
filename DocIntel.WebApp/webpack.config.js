const path = require('path');
const webpack = require('webpack');
const globImporter = require('node-sass-glob-importer');
const ExtractTextPlugin = require('extract-text-webpack-plugin');
const extractCSS = new ExtractTextPlugin('[name].css');

module.exports = {
  mode: 'development',
  entry: { 
      'main': './wwwroot/src/js/dist2.js'
  },  
  output: {
    path: path.resolve(__dirname, 'wwwroot/dist'),
    filename: '[name].js',
    libraryTarget: 'var',
    library: 'DocIntel'
  },
  optimization: {
    splitChunks: { chunks: "all" }
  },
  plugins: [
    extractCSS,
    require('autoprefixer'),
    new webpack.ProvidePlugin({
      'window.jQuery': 'jquery',
      'window.$': 'jquery',
      'jQuery': 'jquery',
      '$': 'jquery',
      CodeMirror: 'codemirror'
    })
  ],/*
  externals: [
      '../../dist2/js/vendors.bundle.js',
      '../../dist2/js/app.bundle.js',
      '../../dist2/js/formplugins/select2/select2.bundle.js',
      '../../dist2/js/formplugins/summernote/summernote.js',
      '../../dist2/js/formplugins/ion-rangeslider/ion-rangeslider.js',
      '../../dist2/js/formplugins/dropzone/dropzone.js'
  ],*/
  module: {
    rules: [
      {
        test : /\/dist2\/js\/[a-z\.]+\.js$/,
        use  : [
          {
            loader : 'imports-loader?this=>window,define=>false'
          }
        ]
      },
      {
        test: /\.js/,
    	exclude: /(dist2|node_modules|bower_components)/,
        use: [{
            loader: 'babel-loader'
        }]
      },
      { 
        test: /\.css$/, 
        use: extractCSS.extract(['css-loader? minimize'])
      },
      {
        test: /\.(jpe?g|png|gif|svg)$/i,
        use: [
          {
            loader: 'file-loader',
            options: {
              name: '[name].[ext]',
              outputPath: 'img/'
            }
          },
          {
            loader: 'image-webpack-loader',
            options: {
              bypassOnDebug: true, // webpack@1.x
              disable: true, // webpack@2.x and newer
            },
	        },
	      ],
      },
      {
        test: /\.(scss)$/,
        use: ExtractTextPlugin.extract({
          fallback: 'style-loader',
          use: ['css-loader', 'postcss-loader', 
          {
            loader: 'sass-loader',
            options: {
              sourceMap: true,
              importer: globImporter()
            }
          }]
        })
      },
      {
        test: /\.(woff(2)?|ttf|eot)(\?v=\d+\.\d+\.\d+)?$/,
        use: [
          {
            loader: 'file-loader',
            options: {
              name: '[name].[ext]',
              outputPath: 'fonts/'
            }
          }
        ]
      }
    ]
  },
  resolve : {
  },
};
