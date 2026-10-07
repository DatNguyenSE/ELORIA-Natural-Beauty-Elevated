import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-story',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './story.html',
  styleUrl: './story.css',
})
export class Story {}
