import type { PluginOptions, PluginConfig } from 'vuepress-vite';

export const plugin: PluginConfig<PluginOptions>[] = [
   [
      '@vuepress/plugin-search',
      {
         // exclude v1 route
         isSearchable: (page) => !page.path.match(/^\/v1\/*.*$/),
      },
   ],
   [
      '@vuepress/plugin-google-analytics',
      {
        id: 'G-LV6WS6HDKN',
      },
   ]
   // [
   //    '@vuepress/plugin-register-components',
   //    {
   //       componentsDir: path.resolve(__dirname, '../components'),
   //    },
   // ]
]
