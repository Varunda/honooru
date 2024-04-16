<template>
    <nav>
        <ul class="pagination justify-content-center">
            <li class="page-item">
                <a class="page-link bi-chevron-double-left" :href="links.first" :class="{ 'disabled': currentPage == 0 }"></a>
            </li>
            <li class="page-item">
                <a class="page-link bi-chevron-left" :href="links.previous" :class="{ 'disabled': currentPage == 0 }"></a>
            </li>

            <li v-for="p in (PageCount)" class="page-item">
                <a class="page-link" :class="{ 'active': p == 1 }" :href="'/posts?q=' + query + '&offset=' + ((p + currentPage - 1) * limit)">
                    {{p + currentPage}}
                </a>
            </li>

            <li class="page-item">
                <a class="page-link bi-chevron-right" :href="links.next" :class="{ 'disabled': nextPageEnabled }"></a>
            </li>
            <li class="page-item">
                <a class="page-link bi-chevron-double-right" :href="links.last" :class="{ 'disabled': nextPageEnabled }"></a>
            </li>
        </ul>
    </nav>
</template>

<script lang="ts">
    import Vue from "vue";

    export const PostsPage = Vue.extend({
        props: {
            query: { type: String, required: true },
            offset: { type: Number, required: true },
            limit: { type: Number, required: true },
            PageCount: { type: Number, required: true }
        },

        computed: {
            links: function() {
                // can't use this.query in the return{} for some reason, but it works out here /shrug
                const query: string = this.query;
                const currentPage: number = this.currentPage;
                const limit: number = this.limit;

                return {
                    first: `/posts?q=${query}`,
                    previous: `/posts?q=${query}&offset=${Math.max(0, currentPage - 1) * limit}`,
                    next: `/posts?q=${query}&offset=${(currentPage + 1) * limit}`,
                    last: ""
                }
            },

            currentPage: function(): number {
                return Math.floor(this.offset / this.limit);
            },

            nextPageEnabled: function(): boolean {
                console.log(`${this.currentPage} ${this.PageCount} ${Math.floor(this.offset / this.limit)}`);
                return this.currentPage > this.PageCount + Math.floor(this.offset / this.limit) - 2;
            }

        }
    });
    export default PostsPage;
</script>