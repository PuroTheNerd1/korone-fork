const fs = require('fs');
const path = require('path');
const pkg = require('./package.json');
const configPath = path.join(__dirname, path.sep + 'config.json');
if (!fs.existsSync(configPath)) {
    throw new Error('Configuration could not be found at location: ' + configPath);
}
const config = JSON.parse(fs.readFileSync(configPath).toString('utf-8'));

const withBundleAnalyzer = require('@next/bundle-analyzer')({
    enabled: process.env.ANALYZE === 'true',
    //analyzerMode: 'json', openAnalyzer: false,
});

module.exports = withBundleAnalyzer({
    reactStrictMode: true,
    swcMinify: false,
    serverRuntimeConfig: config.serverRuntimeConfig,
    publicRuntimeConfig: {
        ...config.publicRuntimeConfig,
        frontendVer: pkg.version,
    },
    async redirects() {
        return [
            {
                source: '/catalog.aspx',
                destination: '/catalog',
                permanent: true,
            },
            /*
            {
              source: '/catalog/:id/:name',
              destination: '/redirect-item?id=:id',
              permanent: false,
            },
             */
            {
                source: '/My/Groups.aspx',
                has: [
                    {
                        type: 'query',
                        key: 'gid',
                        value: '(?<id>.*)',
                    },
                ],
                destination: '/groups/:id/--',
                permanent: true,
            },
        ]
    },
    webpack(config, { isServer }) {
        config.plugins = config.plugins.filter(plugin => {
            return plugin.constructor.name !== 'ReactFreshWebpackPlugin';
        });
        if (!isServer) {
            config.resolve.fallback = {
                ...(config.resolve.fallback || {}),
                crypto: false,
            };
            config.externals = config.externals || {};
            config.externals['axios'] = 'axios';
        }

        return config;
    }
})
