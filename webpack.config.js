﻿// Template for webpack.config.js in Fable projects
// Find latest version in https://github.com/fable-compiler/webpack-config-template

// In most cases, you'll only need to edit the CONFIG object (after dependencies)
// See below if you need better fine-tuning of Webpack options

// Dependencies. Also required: core-js, fable-loader, fable-compiler, @babel/core,
// @babel/preset-env, babel-loader, sass, sass-loader, css-loader, style-loader, file-loader
const path = require('path');
const webpack = require('webpack');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const CopyWebpackPlugin = require('copy-webpack-plugin');
const BundleAnalyzerPlugin = require('webpack-bundle-analyzer').BundleAnalyzerPlugin;
const TerserPlugin = require('terser-webpack-plugin');

var CONFIG = {
    // The tags to include the generated JS and CSS will be automatically injected in the HTML template
    // See https://github.com/jantimon/html-webpack-plugin
    indexHtmlTemplate: './src/client/public/index.html',
    fsharpEntry: './src/client/client.fsproj',
    outputDir: './src/client/deploy',
    assetsDir: './src/client/public',
    devServerHost: 'localhost',
    devServerPort: 8888,
    // When using webpack-dev-server, you may need to redirect some calls
    // to a external API server. See https://webpack.js.org/configuration/dev-server/#devserver-proxy
    devServerProxy: {
        // redirect requests that start with /api/* to the server on port 8085
        '/api/*': {
            target: 'http://localhost:' + (process.env.SERVER_PROXY_PORT || "8085"),
               changeOrigin: true
           }
       },
    // Use babel-preset-env to generate JS compatible with most-used browsers.
    // More info at https://babeljs.io/docs/en/next/babel-preset-env.html
    babel: {
        presets: [
            ['@babel/preset-env', {
                modules: false,
                // This adds polyfills when needed. Requires core-js dependency.
                // See https://babeljs.io/docs/en/babel-preset-env#usebuiltins
                useBuiltIns: 'usage',
                corejs: 3
            }]
        ],
    }
}

// If we're running the webpack-dev-server, assume we're in development mode
var isProduction = !process.argv.find(v => v.indexOf('webpack-dev-server') !== -1);
console.log('Bundling for ' + (isProduction ? 'production' : 'development') + '...');

// The HtmlWebpackPlugin allows us to use a template for the index.html page
// and automatically injects <script> or <link> tags for generated bundles.
var commonPlugins = [
    new HtmlWebpackPlugin({
        filename: 'index.html',
        template: resolve(CONFIG.indexHtmlTemplate)
    })
];

module.exports = {
    // In development, split the JavaScript and CSS files in order to
    // have a faster HMR support. In production bundle styles together
    // with the code because the MiniCssExtractPlugin will extract the
    // CSS in a separate files.
    entry: isProduction ? {
        app: [
            resolve(CONFIG.fsharpEntry)
            // resolve(CONFIG.cssEntry)
        ]
    } : {
            app: [resolve(CONFIG.fsharpEntry)]
            // style: [resolve(CONFIG.cssEntry)]
        },
    // Add a hash to the output file name in production
    // to prevent browser caching if code changes
    output: {
        path: resolve(CONFIG.outputDir),
        pathinfo: !isProduction,
        filename: isProduction ? '[name].[hash].js' : '[name].js'
    },
    mode: isProduction ? 'production' : 'development',
    devtool: isProduction ? 'hidden-source-map' : /*'inline-cheap-source-map'*/ /*'eval'*/ 'eval-source-map',
    context: resolve('.'),
    optimization: {
        splitChunks: {
            chunks: 'all'
        },
        minimizer: [
            new TerserPlugin({
              cache: true,
              parallel: true,
              sourceMap: true, // Must be set to true if using source-maps in production
              terserOptions: {
                // https://github.com/webpack-contrib/terser-webpack-plugin#terseroptions
                toplevel: true,
                mangle: false,
                keep_classnames: true,
                keep_fnames: true,
              }
            }),
        ],
    },
    // Besides the HtmlPlugin, we use the following plugins:
    // PRODUCTION
    //      - MiniCssExtractPlugin: Extracts CSS from bundle to a different file
    //          To minify CSS, see https://github.com/webpack-contrib/mini-css-extract-plugin#minimizing-for-production
    //      - CopyWebpackPlugin: Copies static assets to output directory
    // DEVELOPMENT
    //      - HotModuleReplacementPlugin: Enables hot reloading when code changes without refreshing
    plugins: isProduction ?
        commonPlugins.concat([
            // new MiniCssExtractPlugin({ filename: 'style.[hash].css' }),
            new CopyWebpackPlugin([{ from: resolve(CONFIG.assetsDir) }])
        ])
        : commonPlugins.concat([
            new webpack.HotModuleReplacementPlugin(),
            new BundleAnalyzerPlugin(),
        ]),
    resolve: {
        // See https://github.com/fable-compiler/Fable/issues/1490
        symlinks: false
    },
    // Configuration for webpack-dev-server
    devServer: {
        publicPath: '/',
        contentBase: resolve(CONFIG.assetsDir),
        host: CONFIG.devServerHost,
        port: CONFIG.devServerPort,
        proxy: CONFIG.devServerProxy,
        hot: true,
        inline: true
    },
    // - fable-loader: transforms F# into JS
    // - babel-loader: transforms JS to old syntax (compatible with old browsers)
    // - file-loader: Moves files referenced in the code (fonts, images) into output folder
    module: {
        rules: [
            {
                test: /\.fs(x|proj)?$/,
                use: {
                    loader: 'fable-loader',
                    options: {
                        babel: CONFIG.babel,
                        define: isProduction ? [] : ["DEBUG"]
                    }
                }
            },
            {
                test: /\.js$/,
                exclude: /node_modules/,
                use: {
                    loader: 'babel-loader',
                    options: CONFIG.babel
                },
            },
            {
                test: /\.(png|jpg|jpeg|gif|svg|woff|woff2|ttf|eot)(\?.*)?$/,
                use: ['file-loader']
            }
        ]
    }
};

function resolve(filePath) {
    return path.isAbsolute(filePath) ? filePath : path.join(__dirname, filePath);
}