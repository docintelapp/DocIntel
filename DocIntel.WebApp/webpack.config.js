const path = require('path');
const webpack = require('webpack');
const MiniCssExtractPlugin = require("mini-css-extract-plugin");

module.exports = {
  mode: 'development',
  entry: { 
    'main': './wwwroot/src/js/common.js'
  },  
  output: {
    path: path.resolve(__dirname, 'wwwroot/dist'),
    filename: '[name].js'
  },
  plugins: [
    new MiniCssExtractPlugin(),
    require('autoprefixer'),
    new webpack.ProvidePlugin({
      $: 'jquery',
      jQuery: 'jquery',
      CodeMirror: 'codemirror',
    })
  ],
  module: {
    rules: [
      {
        test: /\.css$/,
        use: [MiniCssExtractPlugin.loader, "css-loader"],
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
        use: [
          MiniCssExtractPlugin.loader, "css-loader", "sass-loader"
        ]
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
  externals : {
    "jquery": "jQuery"
  },
};
