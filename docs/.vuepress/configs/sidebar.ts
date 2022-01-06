import type { SidebarConfig } from '@vuepress/theme-default'

export const sidebar: SidebarConfig = {
   '/guide/': [
      {
         text: 'Git hooks',
         children: [
            '/guide/README.md',
            '/guide/getting-started.md',
            '/guide/automate.md',
         ],
      },
      {
         text: 'Task Runner',
         children: [
            '/guide/task-runner.md',
            '/guide/task-configuration.md',
         ]
      },
      {
         text: 'CSharp Scripts',
         children: [
            '/guide/csharp-script.md',
         ]
      },
      // {
      //    text: 'Advanced',
      //    children: [
      //       '/guide/compile.md',
      //       '/guide/entity-framework.md',
      //       '/guide/autoMapper.md',
      //    ]
      // }
   ],
   '/contribution/': [
      {
         text: 'Contribution',
         link: '/contribution/',
         children: [
            '/contribution/README.md',
         ]
      }
   ]
}
