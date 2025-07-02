
import http from 'k6/http';

export const options = {
    vus: 12,
    duration: '10s',
};

export default function () {
    const url = `http://localhost:5000/sum-by-category-${__ENV.VERSION}?category=Electronics`

    http.get(url);
}