import { defineUserConfig } from 'vuepress-vite'
import type { DefaultThemeOptions } from 'vuepress-vite'
import type { ViteBundlerOptions } from '@vuepress/bundler-vite'
import { plugin, themeConfig, head } from './configs'
import { path } from '@vuepress/utils'

export default defineUserConfig<DefaultThemeOptions, ViteBundlerOptions>({
   lang: 'en-US',
   title: 'Husky.Net',
   description: 'Git hooks made easy with husky.net task runner',
   bundler: '@vuepress/bundler-vite',
   plugins: plugin,
   themeConfig: themeConfig,
   port: 3000,
   base: '/Husky.Net/',
   head: head,
   markdown: {
      importCode: {
        handleImportPath: (str) => str
           .replace(/^@/, path.resolve(__dirname, '../../'))
           .replace(/^@src/, path.resolve(__dirname, '../../src/'))
           .replace(/^@husky/, path.resolve(__dirname, '../../src/Husky'))
      },
    },
})
